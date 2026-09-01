using System.Reflection;
using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using MudBlazor;
using MudX.UnitTests.Viewer.TestComponents.SecurityCode;
using MudX.Utilities;
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
            var moduleMock = jsInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
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
                .Arguments[0].Should().Be(AssemblyInfo.ModulePath("mudxSecurityCode.js"));

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
        public void SecurityCode_ShouldRenderAccessibleGroupSemantics()
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Label, "Verification code")
                .Add(p => p.HelperText, "Enter the code from your authenticator.")
                .Add(p => p.Required, true)
                .Add(p => p.Error, true)
                .Add(p => p.ErrorText, "The code is invalid."));

            // Assert
            var group = comp.Find(".mudx-code-container[role='group']");
            comp.FindAll("[role='group']").Should().ContainSingle();
            var labelId = group.GetAttribute("aria-labelledby");
            labelId.Should().NotBeNullOrWhiteSpace();
            comp.FindAll($"#{labelId}").Should().ContainSingle()
                .Which.TextContent.Should().Be("Verification code");

            group.HasAttribute("aria-describedby").Should().BeFalse();
            group.HasAttribute("aria-required").Should().BeFalse();
            group.HasAttribute("aria-invalid").Should().BeFalse();

            var inputs = comp.FindAll("input:not([readonly])");
            inputs.Should().HaveCount(4);
            inputs.Select(input => input.GetAttribute("aria-label")).Should().Equal(
                "Character 1 of 4",
                "Character 2 of 4",
                "Character 3 of 4",
                "Character 4 of 4");
            inputs.Should().OnlyContain(input => input.GetAttribute("aria-required") == "true");
            inputs.Should().OnlyContain(input => input.GetAttribute("aria-invalid") == "true");
            foreach (var input in inputs)
            {
                var descriptionIds = input.GetAttribute("aria-describedby")!
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                descriptionIds.Should().HaveCount(2);
                descriptionIds.Select(id => comp.Find($"#{id}").TextContent)
                    .Should().BeEquivalentTo("Enter the code from your authenticator.", "The code is invalid.");
            }

            comp.FindAll("[role='alert']").Should().ContainSingle()
                .Which.TextContent.Should().Be("The code is invalid.");
            comp.Markup.Split("Verification code").Should().HaveCount(2);
            comp.Markup.Split("Enter the code from your authenticator.").Should().HaveCount(2);
            comp.Markup.Split("The code is invalid.").Should().HaveCount(2);
        }

        [Test]
        public void SecurityCode_ShouldPreferExplicitAriaLabelForAccessibleName()
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Label, "Visible label")
                .Add(p => p.AriaLabel, "Account verification code"));

            // Assert
            var group = comp.Find(".mudx-code-container[role='group']");
            group.GetAttribute("aria-label").Should().Be("Account verification code");
            group.HasAttribute("aria-labelledby").Should().BeFalse();
            comp.Markup.Split("Visible label").Should().HaveCount(2);
        }

        [Test]
        public void SecurityCode_ShouldKeepGroupAndSegmentAriaLabelOwnershipSeparate()
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.AriaLabel, "Account verification code")
                .Add(p => p.UserAttributes, new Dictionary<string, object?>
                {
                    ["aria-label"] = "Custom segment"
                }));

            // Assert
            comp.Find(".mudx-code-container[role='group']")
                .GetAttribute("aria-label").Should().Be("Account verification code");
            comp.FindAll("input:not([readonly])")
                .Should().OnlyContain(input => input.GetAttribute("aria-label") == "Custom segment");
        }

        [Test]
        public void SecurityCode_ShouldAssociateVisibleLabelWithFirstEditableSegment()
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "-##")
                .Add(p => p.Label, "Verification code"));

            // Assert
            var label = comp.Find("label.mudx-code-label");
            var firstEditableInput = comp.Find("input:not([readonly])");
            label.GetAttribute("for").Should().Be(firstEditableInput.Id);
            label.GetAttribute("for").Should().NotBe(comp.Find("input[readonly]").Id);
        }

        [Test]
        public void SecurityCode_ShouldOmitInactiveRequiredAndErrorSemantics()
        {
            // Act
            var comp = Context.RenderComponent<MudXSecurityCode>();

            // Assert
            var group = comp.Find(".mudx-code-container[role='group']");
            group.HasAttribute("aria-required").Should().BeFalse();
            group.HasAttribute("aria-invalid").Should().BeFalse();
            group.HasAttribute("aria-describedby").Should().BeFalse();
            comp.FindAll("[role='group']").Should().ContainSingle();
            comp.FindAll("input:not([readonly])").Should().OnlyContain(input =>
                input.GetAttribute("aria-required") == "false"
                && input.GetAttribute("aria-invalid") == "false"
                && !input.HasAttribute("aria-describedby"));
        }

        [Test]
        public async Task SecurityCode_ShouldAnnounceErrorOnceWhenErrorStateChanges()
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.HelperText, "Enter the code from your authenticator.")
                .Add(p => p.ErrorText, "The code is invalid."));

            // Act
            await comp.InvokeAsync(() => comp.SetParametersAndRender(parameters => parameters
                .Add(p => p.Error, true)));

            // Assert
            var inputs = comp.FindAll("input:not([readonly])");
            inputs.Should().OnlyContain(input => input.GetAttribute("aria-invalid") == "true");
            inputs.Should().OnlyContain(input => input.GetAttribute("aria-describedby")!
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => comp.Find($"#{id}").TextContent)
                .Contains("The code is invalid."));
            comp.FindAll("[role='alert']").Should().ContainSingle()
                .Which.TextContent.Should().Be("The code is invalid.");
        }

        [Test]
        public void SecurityCode_ShouldDisableEveryEditableSegment()
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "#-#")
                .Add(p => p.Disabled, true));

            // Assert
            var group = comp.Find(".mudx-code-container[role='group']");
            group.GetAttribute("aria-disabled").Should().Be("true");
            comp.FindAll("input:not([readonly])").Should().HaveCount(2)
                .And.OnlyContain(input => input.HasAttribute("disabled"));
            comp.Find("input[readonly]").HasAttribute("disabled").Should().BeFalse();
        }

        [Test]
        public void SecurityCode_ShouldKeepFixedPatternCharactersOutOfSequentialFocus()
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "#-#"));

            // Assert
            comp.FindAll("input:not([readonly])").Should().HaveCount(2)
                .And.OnlyContain(input => !input.HasAttribute("tabindex"));
            var fixedInput = comp.Find("input[readonly]");
            fixedInput.GetAttribute("tabindex").Should().Be("-1");
            fixedInput.GetAttribute("aria-hidden").Should().Be("true");
            fixedInput.HasAttribute("inert").Should().BeTrue();
        }

        [Test]
        public void SecurityCode_ShouldSupportLocalizedSegmentNames()
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.SegmentAriaLabelFormat, "Caractère {0} sur {1}"));

            // Assert
            comp.FindAll("input:not([readonly])")
                .Select(input => input.GetAttribute("aria-label"))
                .Should().Equal(
                    "Caractère 1 sur 4",
                    "Caractère 2 sur 4",
                    "Caractère 3 sur 4",
                    "Caractère 4 sur 4");
        }

        [Test]
        public void SecurityCode_ShouldExposeNullableSegmentAriaLabelFormat()
        {
            // Arrange
            var property = typeof(MudXSecurityCode).GetProperty(nameof(MudXSecurityCode.SegmentAriaLabelFormat));

            // Act
            var nullability = new NullabilityInfoContext().Create(property!);

            // Assert
            nullability.ReadState.Should().Be(NullabilityState.Nullable);
            nullability.WriteState.Should().Be(NullabilityState.Nullable);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("Character {0")]
        [TestCase("Character {2} of {1}")]
        public void SecurityCode_ShouldFallbackWhenSegmentAriaLabelFormatIsInvalid(string? format)
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.SegmentAriaLabelFormat, format));

            // Assert
            comp.FindAll("input:not([readonly])")
                .Select(input => input.GetAttribute("aria-label"))
                .Should().Equal(
                    "Character 1 of 4",
                    "Character 2 of 4",
                    "Character 3 of 4",
                    "Character 4 of 4");
        }

        [Test]
        public void SecurityCode_ShouldPreserveExplicitSegmentAriaLabel()
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.SegmentAriaLabelFormat, "Character {0")
                .Add(p => p.UserAttributes, new Dictionary<string, object?>
                {
                    ["aria-label"] = "Custom segment"
                }));

            // Assert
            comp.FindAll("input:not([readonly])")
                .Should().OnlyContain(input => input.GetAttribute("aria-label") == "Custom segment");
        }

        [Test]
        public async Task SecurityCode_ShouldAllowEmptyOptionalSegments()
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>();
            var fields = comp.FindComponents<MudTextField<string>>();

            // Act
            await comp.InvokeAsync(() => Task.WhenAll(fields.Select(field => field.Instance.ValidateAsync())));

            // Assert
            fields.Should().OnlyContain(field => !field.Instance.HasErrors);
        }

        [Test]
        public async Task SecurityCode_ShouldRejectEmptyRequiredSegments()
        {
            // Arrange
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Required, true));
            var fields = comp.FindComponents<MudTextField<string>>();

            // Act
            await comp.InvokeAsync(() => Task.WhenAll(fields.Select(field => field.Instance.ValidateAsync())));

            // Assert
            fields.Should().OnlyContain(field => field.Instance.HasErrors);
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
            comp.Instance.CodeItems[0].Value.Should().Be(expectedValue[..1]);

            // reset value and Items (ensure onchangehandler happens)
            await comp.InvokeAsync(async () => await comp.Instance._codeState.SetValueAsync(default));
            comp.WaitForAssertion(() => comp.Instance._codeState.Value.Should().Be(null));
            comp.Instance.CodeItems[0].Value = string.Empty; // make sure items are reset

            // start paste at position 1
            await comp.InvokeAsync(async () => await comp.Instance.ClipboardPasteEvent("mudx-code-1-random-guid", pasteText));
            comp.WaitForAssertion(() => comp.Instance._codeState.Value.Should().Be(expectedValue2));
            comp.Instance.CodeItems[1].Value.Should().Be(expectedValue[..1]);
        }

        [Test]
        [Ignore("Skipping this test temporarily")]
        public async Task SecurityCode_ShouldUpdateCodeWhenCodeItemIsRemoved()
        {
            // Arrange
            var comp = Context.RenderComponent<SecurityCodeBasicTest>();
            var codeComp = comp.FindComponent<MudXSecurityCode>();

            // Assert
            codeComp.Should().NotBeNull();
            codeComp.Instance.CodeItems.Count.Should().Be(4);

            var inputs = await comp.InvokeAsync(() => comp.FindAll(".mudx-code-item input"));
            inputs.Count.Should().Be(4);

            await comp.InvokeAsync(() => inputs[0].Input("1"));
            await comp.InvokeAsync(() => inputs[1].Input("2"));
            await comp.InvokeAsync(() => inputs[2].Input("3"));
            await comp.InvokeAsync(() => inputs[3].Input("4"));

            comp.WaitForAssertion(() => comp.Find(".mud-info-text").GetInnerText().Should().Be("Security Code: 1234"));
            codeComp.Instance._codeState.Value.Should().Be("1234");

            inputs = await comp.InvokeAsync(() => comp.FindAll(".mudx-code-item input"));
            await comp.InvokeAsync(() => inputs[3].Change(string.Empty)); // remove last item

            // Re-fetch inputs after re-render
            comp.WaitForAssertion(() =>
            {
                inputs = comp.FindAll(".mudx-code-item input");
                inputs.Count.Should().Be(3);
            });

            await comp.InvokeAsync(() => inputs[^1].Change(string.Empty)); // remove last item

            comp.WaitForAssertion(() => comp.Find(".mud-info-text").GetInnerText().Should().Be("Security Code: 123"));
            comp.WaitForAssertion(() => comp.Find(".mud-input-error").Should().NotBeNull()); // should have an error class
        }
    }
}
