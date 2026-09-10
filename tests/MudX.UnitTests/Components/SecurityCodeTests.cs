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

        /// <summary>
        /// A code mutation emits exactly one <see cref="MudXSecurityCode.CodeChanged"/> notification.
        /// </summary>
        [Test]
        public async Task SecurityCode_CodeMutation_NotifiesOnce()
        {
            var notifications = new List<string?>();
            var module = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            module.Setup<bool>("init", _ => true);
            module.Setup<bool>("focusBlock", _ => true);
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(x => x.CodeChanged, value => notifications.Add(value)));

            await comp.InvokeAsync(async () =>
            {
                comp.Instance.CodeItems[0].Value = "1";
                await comp.Instance.OnAfterChange(0);
            });

            notifications.Should().Equal("1");
        }

        /// <summary>
        /// Backspace on an empty slot removes the previous editable value in the same operation, including across literals.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public async Task SecurityCode_BackspaceOnEmpty_RemovesPreviousEditableValue(bool password)
        {
            var notifications = new List<string?>();
            var module = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            module.Setup<bool>("init", _ => true);
            module.Setup<bool>("focusBlock", _ => true);
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(x => x.Pattern, "##-##-##")
                .Add(x => x.Password, password)
                .Add(x => x.CodeChanged, value => notifications.Add(value)));
            SetValues(comp.Instance, (0, "1"), (1, "2"), (3, "3"), (4, "4"), (6, "5"));

            var focusTarget = await comp.InvokeAsync(() => InvokeKeyboardEvent(comp.Instance, comp.Instance.CodeItems[7].InputId, "Backspace"));

            comp.Instance.CodeItems[6].Value.Should().BeEmpty();
            comp.Instance.CodeItems[5].Value.Should().Be("-");
            comp.Instance._codeState.Value.Should().Be("12-34-");
            notifications.Should().Equal("12-34-");
            focusTarget.Should().Be(comp.Instance.CodeItems[6].InputId);
            module.VerifyNotInvoke("focusBlock");
        }

        /// <summary>
        /// Backspace on a filled slot clears only that slot and focuses the previous editable slot.
        /// </summary>
        [Test]
        public async Task SecurityCode_BackspaceOnFilled_RemovesCurrentValue()
        {
            var notifications = new List<string?>();
            var module = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            module.Setup<bool>("init", _ => true);
            module.Setup<bool>("focusBlock", _ => true);
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(x => x.Pattern, "##-##-##")
                .Add(x => x.CodeChanged, value => notifications.Add(value)));
            SetValues(comp.Instance, (0, "1"), (1, "2"), (3, "3"), (4, "4"), (6, "5"), (7, "6"));

            var focusTarget = await comp.InvokeAsync(() => InvokeKeyboardEvent(comp.Instance, comp.Instance.CodeItems[7].InputId, "Backspace"));

            comp.Instance.CodeItems[7].Value.Should().BeEmpty();
            comp.Instance.CodeItems[6].Value.Should().Be("5");
            comp.Instance._codeState.Value.Should().Be("12-34-5");
            notifications.Should().Equal("12-34-5");
            focusTarget.Should().Be(comp.Instance.CodeItems[6].InputId);
            module.VerifyNotInvoke("focusBlock");
        }

        /// <summary>
        /// Delete clears only the current editable slot and retains focus there.
        /// </summary>
        [Test]
        public async Task SecurityCode_Delete_RemovesCurrentValueOnly()
        {
            var notifications = new List<string?>();
            var module = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            module.Setup<bool>("init", _ => true);
            module.Setup<bool>("focusBlock", _ => true);
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(x => x.Pattern, "##-##-##")
                .Add(x => x.CodeChanged, value => notifications.Add(value)));
            SetValues(comp.Instance, (0, "1"), (1, "2"), (3, "3"), (4, "4"), (6, "5"), (7, "6"));

            var focusTarget = await comp.InvokeAsync(() => InvokeKeyboardEvent(comp.Instance, comp.Instance.CodeItems[4].InputId, "Delete"));

            comp.Instance.CodeItems[4].Value.Should().BeEmpty();
            comp.Instance.CodeItems[3].Value.Should().Be("3");
            comp.Instance.CodeItems[6].Value.Should().Be("5");
            comp.Instance._codeState.Value.Should().Be("12-3-56");
            notifications.Should().Equal("12-3-56");
            focusTarget.Should().Be(comp.Instance.CodeItems[4].InputId);
            module.VerifyNotInvoke("focusBlock");
        }

        /// <summary>
        /// Arrow keys navigate editable neighbors across literal segments without changing the code.
        /// </summary>
        [TestCase("ArrowLeft", 6, 4)]
        [TestCase("ArrowRight", 4, 6)]
        public async Task SecurityCode_ArrowKey_NavigatesWithoutMutation(string key, int currentIndex, int expectedIndex)
        {
            var notifications = new List<string?>();
            var module = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            module.Setup<bool>("init", _ => true);
            module.Setup<bool>("focusBlock", _ => true);
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(x => x.Pattern, "##-##-##")
                .Add(x => x.CodeChanged, value => notifications.Add(value)));
            SetValues(comp.Instance, (0, "1"), (1, "2"), (3, "3"), (4, "4"), (6, "5"), (7, "6"));

            var focusTarget = await comp.InvokeAsync(() => InvokeKeyboardEvent(comp.Instance, comp.Instance.CodeItems[currentIndex].InputId, key));

            comp.Instance.CodeItems.Where(x => x.IsEditable).Select(x => x.Value).Should().Equal("1", "2", "3", "4", "5", "6");
            notifications.Should().BeEmpty();
            focusTarget.Should().Be(comp.Instance.CodeItems[expectedIndex].InputId);
            module.VerifyNotInvoke("focusBlock");
        }

        /// <summary>
        /// Tab and key events targeting literal segments do not mutate values or steal focus.
        /// </summary>
        [TestCase("Tab", 4)]
        [TestCase("Backspace", 2)]
        [TestCase("Delete", 2)]
        public async Task SecurityCode_UnhandledOrLiteralKey_DoesNothing(string key, int index)
        {
            var notifications = new List<string?>();
            var module = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            module.Setup<bool>("init", _ => true);
            module.Setup<bool>("focusBlock", _ => true);
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(x => x.Pattern, "##-##-##")
                .Add(x => x.CodeChanged, value => notifications.Add(value)));
            SetValues(comp.Instance, (0, "1"), (1, "2"), (3, "3"), (4, "4"), (6, "5"), (7, "6"));

            var focusTarget = await comp.InvokeAsync(() => InvokeKeyboardEvent(comp.Instance, comp.Instance.CodeItems[index].InputId, key));

            comp.Instance.CodeItems.Select(x => x.Value).Should().Equal("1", "2", "-", "3", "4", "-", "5", "6");
            notifications.Should().BeEmpty();
            focusTarget.Should().BeNull();
            module.VerifyNotInvoke("focusBlock");
        }

        /// <summary>
        /// Backspace stops at the first editable boundary and clears at most that first value.
        /// </summary>
        [TestCase("1", "", 1)]
        [TestCase("", null, 0)]
        public async Task SecurityCode_BackspaceAtFirstEditable_StopsAtBoundary(string initialValue, string? expectedNotification, int expectedNotifications)
        {
            var notifications = new List<string?>();
            var module = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            module.Setup<bool>("init", _ => true);
            module.Setup<bool>("focusBlock", _ => true);
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(x => x.CodeChanged, value => notifications.Add(value)));
            SetValues(comp.Instance, (0, initialValue));

            var focusTarget = await comp.InvokeAsync(() => InvokeKeyboardEvent(comp.Instance, comp.Instance.CodeItems[0].InputId, "Backspace"));

            comp.Instance.CodeItems[0].Value.Should().BeEmpty();
            notifications.Should().HaveCount(expectedNotifications);
            if (expectedNotifications > 0)
            {
                notifications.Should().Equal(expectedNotification);
            }
            focusTarget.Should().Be(comp.Instance.CodeItems[0].InputId);
            module.VerifyNotInvoke("focusBlock");
        }

        /// <summary>
        /// The keyboard bridge accepts only an exact input identifier owned by this component.
        /// </summary>
        [Test]
        public async Task SecurityCode_KeyboardBridge_RejectsMalformedIds()
        {
            var notifications = new List<string?>();
            var module = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            module.Setup<bool>("init", _ => true);
            module.Setup<bool>("focusBlock", _ => true);
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(x => x.CodeChanged, value => notifications.Add(value)));
            SetValues(comp.Instance, (0, "1"));
            var validId = comp.Instance.CodeItems[0].InputId;
            var masterId = comp.Instance.CodeItems[0].MasterId;
            var invalidIds = new[]
            {
                $"xxxxxxxxxx0-{masterId}",
                "mudX-code-0-",
                $"mudX-code--1-{masterId}",
                $"mudX-code-999-{masterId}",
                $"{validId}-extra"
            };

            foreach (var invalidId in invalidIds)
            {
                var focusTarget = await comp.InvokeAsync(() => InvokeKeyboardEvent(comp.Instance, invalidId, "Backspace"));
                focusTarget.Should().BeNull();
            }

            comp.Instance.CodeItems[0].Value.Should().Be("1");
            notifications.Should().BeEmpty();
            module.VerifyNotInvoke("focusBlock");
        }

        /// <summary>
        /// A real input identifier from another component cannot cross the bridge boundary.
        /// </summary>
        [Test]
        public async Task SecurityCode_KeyboardBridge_RejectsAnotherComponentInputId()
        {
            var notifications = new List<string?>();
            var module = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            module.Setup<bool>("init", _ => true);
            module.Setup<bool>("focusBlock", _ => true);
            var target = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(x => x.CodeChanged, value => notifications.Add(value)));
            var other = Context.RenderComponent<MudXSecurityCode>();
            SetValues(target.Instance, (0, "1"));

            var focusTarget = await target.InvokeAsync(() => InvokeKeyboardEvent(target.Instance, other.Instance.CodeItems[0].InputId, "Backspace"));

            focusTarget.Should().BeNull();
            target.Instance.CodeItems[0].Value.Should().Be("1");
            notifications.Should().BeEmpty();
            module.VerifyNotInvoke("focusBlock");
        }

        private static async Task<string?> InvokeKeyboardEvent(MudXSecurityCode component, string inputId, string key)
        {
            var method = typeof(MudXSecurityCode).GetMethod("HandleKeyboardEvent", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();
            var task = method!.Invoke(component, [inputId, key]) as Task<string?>;
            task.Should().NotBeNull();
            return await task!;
        }

        private static void SetValues(MudXSecurityCode component, params (int Index, string Value)[] values)
        {
            foreach (var (index, value) in values)
            {
                component.CodeItems[index].Value = value;
            }
        }
    }
}
