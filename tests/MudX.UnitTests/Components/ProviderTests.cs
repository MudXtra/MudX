using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using Moq;
using MudX.Utilities;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace MudX.UnitTests.Components
{
    [TestFixture]
    public class ProviderTests : BunitTest
    {

        [Test]
        public void ProviderShouldExecuteJavascript()
        {
            // Arrange: Setup JSInterop to expect the import and initialize calls
            var jsInterop = Context.JSInterop;

            // Setup the import call to return a mock module
            var moduleMock = jsInterop.SetupModule(AssemblyInfo.ModulePath("mudxProvider.js"));

            // Setup the initialize call to return true
            moduleMock.Setup<bool>("initialize", _ => true);

            // Act: Render the component
            var comp = Context.Render<MudXProvider>();
            var div = comp.Find(".mudx-provider");
            div.Should().NotBeNull();

            // Assert: Verify the JS module was imported
            jsInterop.VerifyInvoke("import")
                .Arguments[0].Should().Be(AssemblyInfo.ModulePath("mudxProvider.js"));

            // Assert: Verify the initialize method was called
            moduleMock.VerifyInvoke("initialize");
        }

        [Test]
        public void ProviderShouldFailIfJSFails()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var comp = Context.Render<MudXProvider>();
            }, "Failed to initialize MudX");
        }

        [Test]
        public async Task ProviderDisposeShouldIgnoreDisconnectedCircuit()
        {
            var module = new Mock<IJSObjectReference>();
            module.Setup(m => m.InvokeAsync<bool>("initialize", It.IsAny<object?[]?>())).ReturnsAsync(true);
            module.Setup(m => m.InvokeAsync<bool>("initialize", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>())).ReturnsAsync(true);
            module.Setup(m => m.DisposeAsync()).Returns(() => ValueTask.FromException(new JSDisconnectedException("circuit disconnected")));
            var js = new Mock<IJSRuntime>();
            js.Setup(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]?>())).ReturnsAsync(module.Object);
            js.Setup(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>())).ReturnsAsync(module.Object);
            Context.Services.RemoveAll<IJSRuntime>();
            Context.Services.AddSingleton(js.Object);

            var comp = Context.Render<MudXProvider>();

            Func<Task> dispose = async () => await comp.Instance.DisposeAsync();
            await dispose.Should().NotThrowAsync();
            module.Verify(m => m.DisposeAsync(), Times.Once);
        }
    }
}
