using AngleSharp.Dom;
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
            Assert.That(item.InputId, Is.EqualTo("mudX-code-0-"));
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
                TextFieldRef = textField,
                MasterId = "unique-guid"
            };

            // Act & Assert
            Assert.That(item.Index, Is.EqualTo(3));
            Assert.That(item.Value, Is.EqualTo("X"));
            Assert.That(item.PatternChar, Is.EqualTo('9'));
            Assert.That(item.IsEditable, Is.True);
            Assert.That(item.InputId, Is.EqualTo("mudX-code-3-unique-guid"));
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

        [Test]
        public void SecurityCode_ShouldRender()
        {
            // Arrange
            var comp = Context.RenderComponent<SecurityCodeBasicTest>();
            var codeComp = comp.FindComponent<MudXSecurityCode>();

            // Assert
            codeComp.Should().NotBeNull();
            codeComp.Instance.CodeItems.Count.Should().Be(4);
            codeComp.Instance.CodeItems.All(item => item.Value == string.Empty).Should().BeTrue();
            codeComp.Instance.CodeItems.All(item => item.IsEditable).Should().BeTrue();
        }

        [Test]
        public void SecurityCode_ShouldRenderWithCustomPattern()
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>(
                parameters => parameters.Add(p => p.Pattern, "#A?@*-")
                // numeric, alpha, alphanumeric, special, any, read-only
            );

            // Assert
            comp.Should().NotBeNull();
            comp.Instance.CodeItems.Count.Should().Be(6);
            comp.Instance.CodeItems.Take(5).All(item => item.Value == string.Empty).Should().BeTrue();
            comp.Instance.CodeItems.Take(5).All(item => item.IsEditable).Should().BeTrue();
            comp.Instance.CodeItems[5].IsEditable.Should().BeFalse(); // the last item is not a Pattern Character, so it should be read-only
            var codeItems = comp.Instance.CodeItems;


            codeItems[0].PatternChar.Should().Be('#');
            codeItems[0].IsEditable.Should().BeTrue();
            codeItems[1].PatternChar.Should().Be('A');
            codeItems[1].IsEditable.Should().BeTrue();
            codeItems[2].PatternChar.Should().Be('?');
            codeItems[2].IsEditable.Should().BeTrue();
            codeItems[3].PatternChar.Should().Be('@');
            codeItems[3].IsEditable.Should().BeTrue();
            codeItems[4].PatternChar.Should().Be('*');
            codeItems[4].IsEditable.Should().BeTrue();
            codeItems[5].PatternChar.Should().Be('-');
            codeItems[5].IsEditable.Should().BeFalse(); // - isn't a one of the Placeholder characters, so it should be read-only
        }

        // Pattern, PasteText, ExpectedValue, ExpectedValue2 (for pasting at index 1)
        [TestCase("####", "1-2=3_4", "1234", "123")] // should ignore non-pattern characters
        [TestCase("####", "1234", "1234", "123")] // standard case
        [TestCase("##/##/####", "01/22/2019", "01/22/2019", "0/12/2201")] // should format the date correctly based on the pattern
        [TestCase("##/##/####", "01222019", "01/22/2019", "0/12/2201")] // should format the date correctly based on the pattern
        [TestCase("##/##/####", "01", "01/", "0/1")] // only show trailing read only characters if an item after it has a value
        [TestCase("##/##/####", "0122", "01/22/", "0/12/2")] // only show trailing read only characters if an item after it has a value
        [TestCase("##/", "12", "12/", "1/")] // should show trailing read only characters if it is completely filled
        [Test]
        public async Task SecurityCode_ShouldFormatPasteText(string pattern, string pasteText, string expectedValue, string expectedValue2)
        {
            var comp = Context.RenderComponent<MudXSecurityCode>(
                parameters => parameters.Add(p => p.Pattern, pattern)
            );
            // starts paste at position 0
            await comp.InvokeAsync(async () => await comp.Instance.ClipboardPasteEvent("mudx-code-0-random-guid", pasteText));
            comp.WaitForAssertion(() => comp.Instance._codeState.Value.Should().Be(expectedValue));
            comp.Instance.CodeItems[0].Value.Should().Be(expectedValue.Substring(0, 1));

            // reset value and Items (ensure onchangehandler happens)
            await comp.InvokeAsync(async () => await comp.Instance._codeState.SetValueAsync(default));
            comp.WaitForAssertion(() => comp.Instance._codeState.Value.Should().Be(null));
            comp.Instance.CodeItems[0].Value = string.Empty; // make sure items are reset

            // start paste at position 1
            await comp.InvokeAsync(async () => await comp.Instance.ClipboardPasteEvent("mudx-code-1-random-guid", pasteText));
            comp.WaitForAssertion(() => comp.Instance._codeState.Value.Should().Be(expectedValue2));
            comp.Instance.CodeItems[1].Value.Should().Be(expectedValue.Substring(0, 1));
        }

        [Test]
        public void SecurityCode_ShouldUpdateCodeWhenCodeItemIsRemoved()
        {
            // Arrange
            var comp = Context.RenderComponent<SecurityCodeBasicTest>();
            var codeComp = comp.FindComponent<MudXSecurityCode>();

            // Assert
            codeComp.Should().NotBeNull();
            codeComp.Instance.CodeItems.Count.Should().Be(4);
            var inputs = comp.FindAll(".mudx-code-item input");

            inputs.Count.Should().Be(4);
            inputs[0].Input("1");
            inputs[1].Input("2");
            inputs[2].Input("3");
            inputs[3].Input("4");
            comp.WaitForAssertion(() => comp.Find(".mud-info-text").GetInnerText().Should().Be("Security Code: 1234"));
            codeComp.Instance._codeState.Value.Should().Be("1234");
            inputs = comp.FindAll(".mudx-code-item input");
            inputs[3].Change(string.Empty); // remove last item
            inputs[3].Input(string.Empty); // remove last item
            comp.WaitForAssertion(() => comp.Find(".mud-info-text").GetInnerText().Should().Be("Security Code: 123"));
            // verify the last item has an invalid state
            comp.Find(".mud-input-error").Should().NotBeNull(); // should have an error class
        }
    }
}
