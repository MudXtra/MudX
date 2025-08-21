using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class SplitterVerticalExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "SplitterVerticalExample.razor",
                Code: @"@namespace MudX.Docs.Splitter

<MudPaper Class=""pa-4"" Elevation=""0"">
    <MudXSplitter Direction=""SplitterDirection.Vertical"" Height=""250px"">
        <StartSplitter>
            <MudTextField @bind-Value=""_text"" Label=""Enter some text"" Variant=""Variant.Outlined"" Immediate FullWidth=""true"" AutoGrow />
        </StartSplitter>
        <EndSplitter>
            <MudText Typo=""Typo.body2"">@resultText</MudText>
        </EndSplitter>
    </MudXSplitter>
</MudPaper>

@code {
    private string? _text;

    private string resultText => string.IsNullOrWhiteSpace(_text)
        ? ""👉 Your Pig Latin text will appear here!""
        : ToPigLatin(_text);

    private string ToPigLatin(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return string.Join("" "", words.Select(word =>
        {
            if (word.Length == 0) return word;

            string vowels = ""aeiouAEIOU"";
            if (vowels.Contains(word[0]))
            {
                return word + ""yay""; // starts with a vowel
            }
            else
            {
                int vowelIndex = word.IndexOfAny(vowels.ToCharArray());
                if (vowelIndex == -1)
                {
                    return word + ""ay""; // no vowels at all
                }
                else
                {
                    string head = word.Substring(0, vowelIndex);
                    string tail = word.Substring(vowelIndex);
                    return tail + head.ToLower() + ""ay"";
                }
            }
        }));
    }
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
