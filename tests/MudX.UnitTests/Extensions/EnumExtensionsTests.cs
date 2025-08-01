using AwesomeAssertions;
using MudX.Extensions;
using NUnit.Framework;

namespace MudX.UnitTests.Extensions
{
    public class EnumExtensionsTests : BunitTest
    {
        private enum TestEnum
        {
            [System.ComponentModel.Description("First Value Description")]
            FirstValue,
            [System.ComponentModel.Description("Second Value Description")]
            SecondValue,
            NoDescription
        }

        [Test]
        public void ToDescription_ReturnsDescriptionAttributeValue_IfPresent()
        {
            // Act
            var desc1 = TestEnum.FirstValue.ToDescription();
            var desc2 = TestEnum.SecondValue.ToDescription();

            // Assert
            desc1.Should().Be("First Value Description");
            desc2.Should().Be("Second Value Description");
        }

        [Test]
        public void ToDescription_ReturnsEnumName_IfNoDescriptionAttribute()
        {
            // Act
            var desc = TestEnum.NoDescription.ToDescription();

            // Assert
            desc.Should().Be("NoDescription");
        }

        [Test]
        public void ToDescription_ReturnsEmptyString_IfNull()
        {
            // Act
            string desc = EnumExtensions.ToDescription(null);

            // Assert
            desc.Should().Be(string.Empty);
        }

        private enum EmptyEnum { }

        [Test]
        public void ToDescription_ReturnsEnumName_IfFieldNotFound()
        {
            // Arrange
            var value = (EmptyEnum)123; // Invalid value, not defined in enum

            // Act
            var desc = value.ToDescription();

            // Assert
            desc.Should().Be("123");
        }
    }
}
