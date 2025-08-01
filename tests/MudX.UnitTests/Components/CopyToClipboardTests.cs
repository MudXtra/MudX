using AwesomeAssertions;
using Bunit;
using MudBlazor;
using MudX.UnitTests.Viewer.TestComponents.CopyToClipboard;
using NUnit.Framework;

namespace MudX.UnitTests.Components
{
    [TestFixture]
    public class CopyToClipboardTests : BunitTest
    {

        [Test]
        public async Task CopyToClipboardAsync_NullOrEmptyText_ShouldReturnNoTextToCopy()
        {
            // Arrange
            var comp = Context.RenderComponent<CopyToClipboardBasicTest>();
            var copy = comp.FindComponent<MudXCopyToClipboard>();
            // Act
            var result1 = await copy.Instance.CopyToClipboardAsync(null);
            var result2 = await copy.Instance.CopyToClipboardAsync(string.Empty);

            // Assert
            result1.Success.Should().BeFalse();
            result1.Message.Should().Be("No text to copy");
            result2.Success.Should().BeFalse();
            result2.Message.Should().Be("No text to copy");
        }

        [Test]
        public async Task CopyToClipboardAsync_JSRuntimeNotAvailable_ShouldReturnError()
        {
            // Arrange
            var comp = Context.RenderComponent<CopyToClipboardBasicTest>();
            var copy = comp.FindComponent<MudXCopyToClipboard>();
            copy.Instance.IsJSRuntimeAvailable = false;
            // Act
            var result = await copy.Instance.CopyToClipboardAsync("test");

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("JSRuntime is not available");
        }

        [Test]
        public async Task CopyToClipboardAsync_ShouldCopyText_WhenJSRuntimeIsAvailable()
        {
            // Arrange: Set JS runtime availability and disable Snackbar
            var comp = Context.RenderComponent<MudXCopyToClipboard>(p => p
                .Add(x => x.Snackbar, false));
            comp.Instance.IsJSRuntimeAvailable = true;

            // Arrange: Setup JSInterop mock for copyToClipboard
            var jsInterop = Context.JSInterop;
            jsInterop.Setup<string>("mudxGeneral.copyToClipboard", "copied text")
                     .SetResult("success");

            // Act: Call the clipboard copy method
            var result = await comp.Instance.CopyToClipboardAsync("copied text");

            // Assert: Result should indicate success
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Copied to clipboard");

            // Assert: JS method should have been invoked
            jsInterop.VerifyInvoke("mudxGeneral.copyToClipboard")
                     .Arguments[0].Should().Be("copied text");
        }

        [Test]
        public async Task CopyToClipboardAsync_SnackbarEnabled_ShouldCallSnackbarService()
        {
            // Arrange: Set JS runtime availability and disable Snackbar
            var provider = Context.RenderComponent<MudSnackbarProvider>();
            var comp = Context.RenderComponent<MudXCopyToClipboard>(p => p
                .Add(x => x.Snackbar, true));
            comp.Instance.IsJSRuntimeAvailable = true;

            // Arrange: Setup JSInterop mock for copyToClipboard
            var jsInterop = Context.JSInterop;
            jsInterop.Setup<string>("mudxGeneral.copyToClipboard", "copied text")
                     .SetResult("success");

            // Act: Call the clipboard copy method
            var result = await comp.Instance.CopyToClipboardAsync("copied text");

            // Assert
            comp.WaitForAssertion(() => result.Success.Should().BeTrue());
            provider.Find("div.mud-snackbar-content-message").TextContent.Trim().Should().Be("Copied to clipboard");
        }
    }
}