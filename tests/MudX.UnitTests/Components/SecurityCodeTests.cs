using Bunit;
using FluentAssertions;
using MudBlazor;
using MudX.UnitTests.Viewer.TestComponents.SecurityCode;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace MudX.UnitTests.Components
{
    public class SecurityCodeTests : BunitTest
    {
        [Test]
        public void Constructor_ShouldSetDefaults()
        {
            // Act
            var item = new CodeItem();

            // Assert
            Assert.That(item.Index, Is.EqualTo(0));
            Assert.That(item.Value, Is.EqualTo(string.Empty));
            Assert.That(item.PatternChar, Is.EqualTo('\0'));
            Assert.That(item.IsEditable, Is.False);
            Assert.That(item.InputId, Is.EqualTo("mudX-code-0"));
            Assert.That(item.TextFieldRef, Is.Null);
        }

        [Test]
        public void Properties_ShouldBeAssignableAndReturnCorrectValues()
        {
            // Arrange
            var textField = new MudTextField<string>();
            var item = new CodeItem
            {
                Index = 3,
                Value = "X",
                PatternChar = '9',
                IsEditable = true,
                TextFieldRef = textField
            };

            // Act & Assert
            Assert.That(item.Index, Is.EqualTo(3));
            Assert.That(item.Value, Is.EqualTo("X"));
            Assert.That(item.PatternChar, Is.EqualTo('9'));
            Assert.That(item.IsEditable, Is.True);
            Assert.That(item.InputId, Is.EqualTo("mudX-code-3"));
            Assert.That(item.TextFieldRef, Is.EqualTo(textField));
        }

        [Test]
        public async Task SecurityCode_Tests_JSModule()
        {
            // Arrange: Setup JSInterop to expect the import and initialize calls
            var jsInterop = Context.JSInterop;

            // Setup the import call to return a mock module
            var moduleMock = jsInterop.SetupModule("./_content/MudX/modules/mudxSecurityCode.js");
            // Setup the initialize call to return true
            moduleMock.Setup<bool>("init", _ => true);
            moduleMock.Setup<bool>("focusBlock", _ => true);
            moduleMock.Setup<bool>("focusNextAfterContainer", _ => true);
            moduleMock.Setup<bool>("cleanup", _ => true);

            var comp = Context.RenderComponent<SecurityCodeBasicTest>();
            var codeComp = comp.FindComponent<MudXSecurityCode>();
            codeComp.Should().NotBeNull();
            var textFields = comp.FindComponents<MudTextField<string>>().Where(x => x.Markup.Contains("mudx-code-item")).ToList();
            textFields.Count.Should().Be(4);

            // Assert: Verify the JS module was imported
            jsInterop.VerifyInvoke("import")
                .Arguments[0].Should().Be("./_content/MudX/modules/mudxSecurityCode.js");

            await comp.InvokeAsync(async () =>
            {
                codeComp.Instance.CodeItems[0].Value = "1";
                await codeComp.Instance.OnAfterChange(0);
            });

            comp.WaitForAssertion(() => moduleMock.VerifyInvoke("focusBlock"));
            await comp.InvokeAsync(async () =>
            {
                codeComp.Instance.CodeItems[1].Value = "2";
                await codeComp.Instance.OnAfterChange(1);
            });

            await comp.InvokeAsync(async () =>
            {
                codeComp.Instance.CodeItems[2].Value = "3";
                await codeComp.Instance.OnAfterChange(2);
            });

            await comp.InvokeAsync(async () =>
            {
                codeComp.Instance.CodeItems[3].Value = "4";
                await codeComp.Instance.OnAfterChange(3);
            });

            // final input has a value should have run next js
            moduleMock.VerifyInvoke("focusNextAfterContainer");
            // dispose the component
            await codeComp.Instance.DisposeAsync();
            comp.WaitForAssertion(() => moduleMock.VerifyInvoke("cleanup"));
        }
    }
}
