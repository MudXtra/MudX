using System.Text;

namespace MudX
{
    /// <summary>
    /// A static class for masking and unmasking IDs using an obfuscation technique for human friendly keyboard inputs.
    /// The returned IDs are not cryptographically secure, but are designed to be human-friendly and easy to type.
    /// The resulting Mask may be longer than the original ID, as ASCII letters are converted to 2-character sequences.
    /// </summary>
    public static class IdMasker
    {
        private static readonly IDictionary<char, char> LetterToDigitIdMappings = new Dictionary<char, char>
        {
            // Digit 0
            ['a'] = '0',
            ['k'] = '0',
            ['u'] = '0',
            ['E'] = '0',
            ['Y'] = '0',

            // Digit 1
            ['b'] = '1',
            ['v'] = '1',
            ['F'] = '1',
            ['P'] = '1',

            // Digit 2
            ['c'] = '2',
            ['m'] = '2',
            ['w'] = '2',
            ['Q'] = '2',

            // Digit 3
            ['d'] = '3',
            ['n'] = '3',
            ['x'] = '3',
            ['H'] = '3',
            ['R'] = '3',

            // Digit 4
            ['e'] = '4',
            ['y'] = '4',
            ['I'] = '4',
            ['T'] = '4',

            // Digit 5
            ['f'] = '5',
            ['p'] = '5',
            ['z'] = '5',
            ['J'] = '5',

            // Digit 6
            ['g'] = '6',
            ['q'] = '6',
            ['A'] = '6',
            ['K'] = '6',
            ['U'] = '6',

            // Digit 7
            ['h'] = '7',
            ['r'] = '7',
            ['L'] = '7',
            ['V'] = '7',

            // Digit 8
            ['i'] = '8',
            ['s'] = '8',
            ['C'] = '8',
            ['M'] = '8',
            ['W'] = '8',

            // Digit 9
            ['j'] = '9',
            ['t'] = '9',
            ['D'] = '9',
            ['N'] = '9',
            ['X'] = '9',
        };

        private static readonly IDictionary<string, char> AlphaToNumericMapping = new Dictionary<string, char>
        {
            ["00"] = 'b',
            ["0."] = 'e',
            ["1."] = 'f',
            ["2."] = 'F',
            ["3."] = 'E',
            ["4."] = 'N',
            ["5."] = 'P',
            ["6."] = 'V',
            ["7."] = 'c',
            ["8."] = 'M',
            ["9."] = 'X',
            ["01"] = 'W',
            ["02"] = 'v',
            ["03"] = 'a',
            ["04"] = 'z',
            ["05"] = 'm',
            ["06"] = 'Z',
            ["07"] = 'p',
            ["08"] = 'b',
            ["09"] = 'f',
            ["10"] = 'U',
            ["11"] = 'M',
            ["12"] = 'V',
            ["13"] = 's',
            ["14"] = 'D',
            ["15"] = 'O',
            ["16"] = 'd',
            ["17"] = 'P',
            ["18"] = 'y',
            ["19"] = 'F',
            ["20"] = 'L',
            ["21"] = 'c',
            ["22"] = 'T',
            ["23"] = 'E',
            ["24"] = 'I',
            ["25"] = 'X',
            ["26"] = 'a',
            ["27"] = 'W',
            ["28"] = 'e',
            ["29"] = 'y',
            ["30"] = 'J',
            ["31"] = 'w',
            ["32"] = 'l',
            ["33"] = 'q',
            ["34"] = 'Z',
            ["35"] = 'l',
            ["36"] = 'C',
            ["37"] = 'o',
            ["38"] = 'n',
            ["39"] = 'g',
            ["40"] = 't',
            ["41"] = 'd',
            ["42"] = 'H',
            ["43"] = 'u',
            ["44"] = 'N',
            ["45"] = 'O',
            ["46"] = 'm',
            ["47"] = 'G',
            ["48"] = 'T',
            ["49"] = 'r',
            ["50"] = 'U',
            ["51"] = 'Y',
            ["52"] = 'K',
            ["53"] = 'J',
            ["54"] = 'x',
            ["55"] = 'v',
            ["56"] = 'h',
            ["57"] = 'j',
            ["58"] = 'H',
            ["59"] = 'e',
            ["60"] = 'k',
            ["61"] = 't',
            ["62"] = 'a',
            ["63"] = 'G',
            ["64"] = 'q',
            ["65"] = 'A',
            ["66"] = 'g',
            ["67"] = 'R',
            ["68"] = 'S',
            ["69"] = 'p',
            ["70"] = 'Q',
            ["71"] = 'C',
            ["72"] = 's',
            ["73"] = 't',
            ["74"] = 'o',
            ["75"] = 'T',
            ["76"] = 'w',
            ["77"] = 'I',
            ["78"] = 'B',
            ["79"] = 'u',
            ["80"] = 'r',
            ["81"] = 'x',
            ["82"] = 'U',
            ["83"] = 'A',
            ["84"] = 'B',
            ["85"] = 'T',
            ["86"] = 'E',
            ["87"] = 'Q',
            ["88"] = 'n',
            ["89"] = 'D',
            ["90"] = 'S',
            ["91"] = 'R',
            ["92"] = 'h',
            ["93"] = 'L',
            ["94"] = 'K',
            ["95"] = 'z',
            ["96"] = 'i',
            ["97"] = 'A',
            ["98"] = 'k',
            ["99"] = 'i',
        };

        // Precompute reverse mappings for efficiency
        private static readonly IDictionary<char, char[]> DigitToLetterMappings = LetterToDigitIdMappings
            .GroupBy(kv => kv.Value)
            .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToArray());

        private static readonly IDictionary<char, string[]> LetterToAlphaMappings = AlphaToNumericMapping
            .GroupBy(kv => kv.Value)
            .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToArray());

        /// <summary>
        /// Creates an obfuscated version of the given string Id. All digits and letters will be obfuscated randomly, other characters will remain unchanged.
        /// </summary>
        /// <param name="id">The input ID string to mask.</param>
        /// <returns>Masked ID string.</returns>
        public static string MaskId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return id;

            var sb = new StringBuilder(id.Length);

            for (int i = 0; i < id.Length;)
            {
                var ch = id[i];
                if (char.IsDigit(ch))
                {
                    if (DigitToLetterMappings.TryGetValue(ch, out var letters))
                    {
                        var letter = letters[Random.Shared.Next(letters.Length)];
                        sb.Append(letter);
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                    i++;
                }
                else if (char.IsAsciiLetter(ch))
                {
                    if (LetterToAlphaMappings.TryGetValue(ch, out var codes))
                    {
                        var code = codes[Random.Shared.Next(codes.Length)];
                        sb.Append(code);
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                    i++;
                }
                else
                {
                    sb.Append(ch);
                    i++;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Decodes an obfuscated string by converting specific letter or digit sequences into their corresponding values.
        /// If no mapping is found for a character or sequence, the character is appended as-is.
        /// </summary>
        /// <param name="encoded">The masked ID string to unmask.</param>
        /// <returns>Unmasked ID string.</returns>
        public static string UnMaskId(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
                return encoded;

            var sb = new StringBuilder(encoded.Length);

            for (int i = 0; i < encoded.Length;)
            {
                var ch = encoded[i];
                var digits = i + 2 <= encoded.Length ? encoded.Substring(i, 2) : null;
                if (LetterToDigitIdMappings.TryGetValue(ch, out var digit))
                {
                    sb.Append(digit);
                    i++;
                }
                else if (digits != null && AlphaToNumericMapping.TryGetValue(digits, out var numericValue))
                {
                    sb.Append(numericValue);
                    i += 2;
                }
                else
                {
                    sb.Append(ch);
                    i++;
                }
            }
            return sb.ToString();
        }
    }
}
