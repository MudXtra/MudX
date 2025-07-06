using Bunit;
using FluentAssertions;
using NUnit.Framework;

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
            var moduleMock = jsInterop.SetupModule("./_content/MudX/modules/mudxProvider.js");

            // Setup the initialize call to return true
            moduleMock.Setup<bool>("initialize", _ => true);

            // Act: Render the component
            var comp = Context.RenderComponent<MudXProvider>();
            var div = comp.Find(".mudx-provider");
            div.Should().NotBeNull();

            // Assert: Verify the JS module was imported
            jsInterop.VerifyInvoke("import")
                .Arguments[0].Should().Be("./_content/MudX/modules/mudxProvider.js");

            // Assert: Verify the initialize method was called
            moduleMock.VerifyInvoke("initialize");
        }
    }
}
