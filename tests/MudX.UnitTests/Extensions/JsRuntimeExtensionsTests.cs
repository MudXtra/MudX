namespace MudX.UnitTests.Extensions
{
    using System.Threading.Tasks;
    using Microsoft.JSInterop;
    using Moq;
    using Xunit;

    namespace MudX.UnitTests.Extensions
    {
        public class JsRuntimeExtensionsTests
        {
            [Fact]
            public async Task CopyToClipboard_ReturnsSuccess_WhenJsRuntimeInvokesSuccessfully()
            {
                // Arrange
                var jsRuntimeMock = new Mock<IJSRuntime>();
                jsRuntimeMock
                    .Setup(j => j.InvokeAsync<string>(
                        "mudxGeneral.copyToClipboard",
                        It.IsAny<object[]>()))
                    .ReturnsAsync("success");

                // Act
                var result = await jsRuntimeMock.Object.CopyToClipboard("test text");

                // Assert
                Assert.Equal("success", result);
            }

            [Fact]
            public async Task CopyToClipboard_ReturnsExceptionMessage_WhenJsExceptionIsThrown()
            {
                // Arrange
                var jsRuntimeMock = new Mock<IJSRuntime>();
                jsRuntimeMock
                    .Setup(j => j.InvokeAsync<string>(
                        "mudxGeneral.copyToClipboard",
                        It.IsAny<object[]>()))
                    .ThrowsAsync(new JSException("Clipboard error"));

                // Act
                var result = await jsRuntimeMock.Object.CopyToClipboard("test text");

                // Assert
                Assert.Equal("Clipboard error", result);
            }

            [Fact]
            public async Task CopyToClipboard_ReturnsNotInitialized_WhenJsRuntimeIsNull()
            {
                // Arrange
                IJSRuntime? jsRuntime = null;

                // Act
                var result = await JsRuntimeExtensions.CopyToClipboard(jsRuntime, "test text");

                // Assert
                Assert.Equal("IJSRuntime not initialized.", result);
            }
        }
    }
}
