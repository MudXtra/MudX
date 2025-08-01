using AwesomeAssertions;
using MudBlazor;
using MudBlazor.Interop;
using MudX.Utilities;
using NUnit.Framework;

namespace MudX.UnitTests.Utilities
{
    public class PagePositionTests : BunitTest
    {
        private BoundingClientRect CreateRect(
            double left, double top, double width, double height)
        {
            return new BoundingClientRect
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height
            };
        }

        [TestCase(Origin.TopLeft, 10, 20, 100, 50, 10, 20)]
        [TestCase(Origin.TopCenter, 10, 20, 100, 50, 60, 20)]
        [TestCase(Origin.TopRight, 10, 20, 100, 50, 110, 20)]
        [TestCase(Origin.CenterLeft, 10, 20, 100, 50, 10, 45)]
        [TestCase(Origin.CenterCenter, 10, 20, 100, 50, 60, 45)]
        [TestCase(Origin.CenterRight, 10, 20, 100, 50, 110, 45)]
        [TestCase(Origin.BottomLeft, 10, 20, 100, 50, 10, 70)]
        [TestCase(Origin.BottomCenter, 10, 20, 100, 50, 60, 70)]
        [TestCase(Origin.BottomRight, 10, 20, 100, 50, 110, 70)]
        public void GetPagePositionFromOrigin_ReturnsExpectedPosition(
            Origin origin, double left, double top, double width, double height, double expectedX, double expectedY)
        {
            // Arrange
            var rect = CreateRect(left, top, width, height);

            // Act
            var (x, y) = PagePosition.GetPagePositionFromOrigin(rect, origin);

            // Assert
            x.Should().BeApproximately(expectedX, 0.0001);
            y.Should().BeApproximately(expectedY, 0.0001);
        }

        [Test]
        public void GetPagePositionFromOrigin_UnknownOrigin_ReturnsCenter()
        {
            // Arrange
            var rect = CreateRect(0, 0, 100, 100);
            var unknownOrigin = (Origin)999;

            // Act
            var (x, y) = PagePosition.GetPagePositionFromOrigin(rect, unknownOrigin);

            // Assert
            x.Should().BeApproximately(50, 0.0001);
            y.Should().BeApproximately(50, 0.0001);
        }

        [Test]
        public void GetPagePositionFromOrigin_ZeroSizeRect_ReturnsCorrectPosition()
        {
            // Arrange
            var rect = CreateRect(5, 5, 0, 0);

            // Act
            var (x, y) = PagePosition.GetPagePositionFromOrigin(rect, Origin.CenterCenter);

            // Assert
            x.Should().BeApproximately(5, 0.0001);
            y.Should().BeApproximately(5, 0.0001);
        }
    }
}
