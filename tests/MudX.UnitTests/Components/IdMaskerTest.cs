using AwesomeAssertions;
using NUnit.Framework;

namespace MudX.UnitTests.Components
{
    [TestFixture]
    public class IdMaskerTest
    {
        [Test]
        public void MaskId_ShouldObfuscateDigitsAndLetters()
        {
            // Digits: Should be replaced by mapped letters
            var input = "12345";
            var masked = IdMasker.MaskId(input);

            masked.Length.Should().Be(input.Length);
            // Each char should be a mapped letter for the digit
            for (int i = 0; i < input.Length; i++)
            {
                var digit = input[i];
                var mapped = IdMasker.MaskId(digit.ToString());
                IdMasker.UnMaskId(mapped).Should().Be(digit.ToString());
            }
        }

        [Test]
        public void MaskId_ShouldObfuscateAsciiLetters()
        {
            // Letters: Should be replaced by mapped number strings
            var input = "abcXYZ";
            var masked = IdMasker.MaskId(input);

            // Each letter should be replaced by a 2-char string
            masked.Length.Should().Be(input.Length * 2);
            var unmasked = IdMasker.UnMaskId(masked);
            unmasked.Should().Be(input);
        }

        [Test]
        public void MaskId_ShouldLeaveOtherCharactersUnchanged()
        {
            var input = "1a-2b_3c";
            var masked = IdMasker.MaskId(input);

            // Non-alphanumeric chars should remain unchanged
            for (int i = 0, j = 0; i < input.Length;)
            {
                if (char.IsDigit(input[i]))
                {
                    j++;
                    i++;
                }
                else if (char.IsAsciiLetter(input[i]))
                {
                    j += 2;
                    i++;
                }
                else
                {
                    masked[j].Should().Be(input[i]);
                    j++;
                    i++;
                }
            }
        }

        [Test]
        public void UnmaskId_ShouldDecodeMaskedDigitsAndLetters()
        {
            // Masked digits
            var digitMask = IdMasker.MaskId("987");
            var digitUnmask = IdMasker.UnMaskId(digitMask);
            digitUnmask.Should().Be("987");

            // Masked letters
            var letterMask = IdMasker.MaskId("abc");
            var letterUnmask = IdMasker.UnMaskId(letterMask);
            letterUnmask.Should().Be("abc");
        }

        [Test]
        public void UnmaskId_ShouldLeaveUnmappedCharactersUnchanged()
        {
            var input = "!@#";
            var unmasked = IdMasker.UnMaskId(input);
            unmasked.Should().Be(input);
        }

        [Test]
        public void MaskId_And_UnmaskId_ShouldBeInverseForMixedInput()
        {
            var input = "A1b2C3!@";
            var masked = IdMasker.MaskId(input);
            var unmasked = IdMasker.UnMaskId(masked);
            unmasked.Should().Be(input);
        }

        [TestCase("1234567890")]
        [TestCase("abcdefgHIJKLMNopqrstuvwxyz")]
        [TestCase("!@#$%^&*()_+-=[]{}|;':\",.<>/?")]
        [TestCase("MudX123!@#Test456")]
        [TestCase("A1b2C3!@#XyZ")]
        [TestCase("123abcXYZ!@#")]
        [TestCase("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
        [TestCase("A0E6C1F79B2D4B6C8F427D9E5F1A2B3C")]
        [Test]
        public void MaskId_ShouldMaskAndUnMaskCorrectly(string input)
        {
            var masked = IdMasker.MaskId(input);
            var unmasked = IdMasker.UnMaskId(masked);
            unmasked.Should().Be(input);
        }
    }
}
