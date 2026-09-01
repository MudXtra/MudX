using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
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

            var comp = Context.Render<SecurityCodeBasicTest>();
            var codeComp = comp.FindComponent<MudXSecurityCode>();
            codeComp.Should().NotBeNull();
            var textFields = comp.FindComponents<MudTextField<string>>().Where(x => x.Markup.Contains("mudx-code-item")).ToList();
            textFields.Count.Should().Be(4);
            // Assert: Verify the JS module was imported
            jsInterop.VerifyInvoke("import")
                .Arguments[0].Should().Be(AssemblyInfo.ModulePath("mudxSecurityCode.js"));

            await comp.FindAll(".mudx-code-item input")[0].InputAsync(new ChangeEventArgs { Value = "1" });

            comp.WaitForAssertion(() => moduleMock.VerifyInvoke("focusBlock"));
            await comp.FindAll(".mudx-code-item input")[1].InputAsync(new ChangeEventArgs { Value = "2" });
            await comp.FindAll(".mudx-code-item input")[2].InputAsync(new ChangeEventArgs { Value = "3" });
            await comp.FindAll(".mudx-code-item input")[3].InputAsync(new ChangeEventArgs { Value = "4" });

            moduleMock.Invocations.Count(invocation => invocation.Identifier == "focusNextAfterContainer").Should().Be(1);
            // dispose the component
            await codeComp.Instance.DisposeAsync();
            comp.WaitForAssertion(() => moduleMock.VerifyInvoke("cleanup"));
        }

        [Test]
        public void SecurityCode_ShouldRender()
        {
            // Arrange
            var comp = Context.Render<SecurityCodeBasicTest>();
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
            var comp = Context.Render<MudXSecurityCode>(
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

        [Test]
        public async Task SecurityCode_ShouldValidateFormAfterTerminalInput()
        {
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters.Add(p => p.Pattern, "#"));
            var form = comp.FindComponent<MudForm>();

            await comp.InvokeAsync(() => comp.Find(".mudx-code-item input").Input("7"));

            comp.Instance._codeState.Value.Should().Be("7");
            form.Instance.IsValid.Should().BeTrue();
        }

        [Test]
        public async Task SecurityCode_ShouldCompleteAfterTerminalInputWithTrailingLiteral()
        {
            var moduleMock = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            moduleMock.Setup<bool>("init", _ => true);
            moduleMock.Setup<bool>("focusBlock", _ => true);
            var completionCount = 0;
            string? completedValue = null;
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "##/")
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, value =>
                    {
                        completionCount++;
                        completedValue = value;
                    })));
            var form = comp.FindComponent<MudForm>();
            await comp.FindAll(".mudx-code-item input")[0].InputAsync(new ChangeEventArgs { Value = "1" });
            await comp.FindAll(".mudx-code-item input")[1].InputAsync(new ChangeEventArgs { Value = "2" });

            comp.Instance._codeState.Value.Should().Be("12/");
            form.Instance.IsValid.Should().BeTrue();
            completedValue.Should().Be("12/");
            completionCount.Should().Be(1);
        }

        [Test]
        public async Task SecurityCode_ShouldPublishEveryTrailingLiteralBeforeCompleting()
        {
            var moduleMock = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            moduleMock.Setup<bool>("init", _ => true);
            moduleMock.Setup<bool>("focusBlock", _ => true);
            var completedValues = new List<string?>();
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "##/-")
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this,
                        value => completedValues.Add(value))));

            await comp.FindAll(".mudx-code-item input")[0].InputAsync(new ChangeEventArgs { Value = "1" });
            await comp.FindAll(".mudx-code-item input")[1].InputAsync(new ChangeEventArgs { Value = "2" });

            comp.Instance._codeState.Value.Should().Be("12/-");
            completedValues.Should().Equal("12/-");
        }

        [Test]
        public async Task SecurityCode_ShouldCompleteWhenEarlierMissingItemIsFilledLast()
        {
            var moduleMock = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            moduleMock.Setup<bool>("init", _ => true);
            moduleMock.Setup<bool>("focusBlock", _ => true);
            var publishedValues = new List<string?>();
            var completedValues = new List<string?>();
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "##")
                    .Add(p => p.CodeChanged, EventCallback.Factory.Create<string?>(this, value => publishedValues.Add(value)))
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, value => completedValues.Add(value))));
            var form = comp.FindComponent<MudForm>();

            await comp.InvokeAsync(() => comp.FindAll(".mudx-code-item input")[1].Input("2"));
            await comp.InvokeAsync(() => comp.FindAll(".mudx-code-item input")[0].Input("1"));

            comp.Instance._codeState.Value.Should().Be("12");
            publishedValues.Should().Contain("12");
            form.Instance.IsValid.Should().BeTrue();
            completedValues.Should().Equal("12");
        }

        [Test]
        public async Task SecurityCode_ShouldPublishAndValidateBeforeCompletingTerminalInput()
        {
            var moduleMock = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            moduleMock.Setup<bool>("init", _ => true);
            moduleMock.Setup<bool>("focusNextAfterContainer", _ => true);
            var eventOrder = new List<string>();
            MudForm? form = null;
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "#")
                    .Add(p => p.CodeChanged, EventCallback.Factory.Create<string?>(this, value => eventOrder.Add($"published:{value}")))
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, value =>
                    {
                        eventOrder.Add($"completed:{value}");
                        form?.IsValid.Should().BeTrue();
                    })));
            form = comp.FindComponent<MudForm>().Instance;

            await comp.InvokeAsync(() => comp.Find(".mudx-code-item input").Input("7"));

            comp.Instance._codeState.Value.Should().Be("7");
            eventOrder.Should().Contain("published:7");
            eventOrder.Last().Should().Be("completed:7");
            moduleMock.Invocations.Should().NotContain(invocation => invocation.Identifier == "focusNextAfterContainer");
        }

        [Test]
        public async Task SecurityCode_ShouldPublishAndValidateBeforeCompletingPaste()
        {
            var moduleMock = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            moduleMock.Setup<bool>("init", _ => true);
            moduleMock.Setup<bool>("focusNextAfterContainer", _ => true);
            var eventOrder = new List<string>();
            MudForm? form = null;
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "##/##")
                    .Add(p => p.CodeChanged, EventCallback.Factory.Create<string?>(this, value => eventOrder.Add($"published:{value}")))
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, value =>
                    {
                        eventOrder.Add($"completed:{value}");
                        form?.IsValid.Should().BeTrue();
                    })));
            form = comp.FindComponent<MudForm>().Instance;

            await comp.InvokeAsync(() => comp.Instance.ClipboardPasteEvent("mudx-code-0-random-guid", "12/34"));

            comp.Instance._codeState.Value.Should().Be("12/34");
            eventOrder.Should().Contain("published:12/34");
            eventOrder.Last().Should().Be("completed:12/34");
            moduleMock.Invocations.Should().NotContain(invocation => invocation.Identifier == "focusNextAfterContainer");
        }

        [Test]
        public async Task SecurityCode_ShouldFocusNextAfterCompletePasteWithoutHandler()
        {
            var moduleMock = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            moduleMock.Setup<bool>("init", _ => true);
            moduleMock.Setup<bool>("focusNextAfterContainer", _ => true);
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters.Add(p => p.Pattern, "##/##"));
            var form = comp.FindComponent<MudForm>();

            await comp.InvokeAsync(() =>
                comp.Instance.ClipboardPasteEvent("mudx-code-0-random-guid", "12/34"));

            comp.Instance._codeState.Value.Should().Be("12/34");
            form.Instance.IsValid.Should().BeTrue();
            moduleMock.Invocations.Count(invocation => invocation.Identifier == "focusNextAfterContainer").Should().Be(1);
        }

        [Test]
        public async Task SecurityCode_ShouldNotCompletePartialPasteAndShouldMoveInternally()
        {
            var moduleMock = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            moduleMock.Setup<bool>("init", _ => true);
            moduleMock.Setup<bool>("focusBlock", _ => true);
            moduleMock.Setup<bool>("focusNextAfterContainer", _ => true);
            var completionCount = 0;
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "####")
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, _ => completionCount++)));

            await comp.InvokeAsync(() => comp.Instance.ClipboardPasteEvent("mudx-code-0-random-guid", "12"));

            comp.Instance._codeState.Value.Should().Be("12");
            completionCount.Should().Be(0);
            moduleMock.VerifyInvoke("focusBlock");
            moduleMock.Invocations.Should().NotContain(invocation => invocation.Identifier == "focusNextAfterContainer");
        }

        [Test]
        public async Task SecurityCode_ShouldNotCompleteInvalidTerminalInput()
        {
            var moduleMock = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            moduleMock.Setup<bool>("init", _ => true);
            moduleMock.Setup<bool>("focusNextAfterContainer", _ => true);
            var completionCount = 0;
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "#")
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, _ => completionCount++)));

            await comp.InvokeAsync(() => comp.Find(".mudx-code-item input").Input("X"));

            comp.Instance._codeState.Value.Should().BeEmpty();
            completionCount.Should().Be(0);
            moduleMock.Invocations.Should().NotContain(invocation => invocation.Identifier == "focusNextAfterContainer");
        }

        [Test]
        public async Task SecurityCode_ShouldAwaitCompletionHandler()
        {
            var handlerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "#")
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this,
                        new Func<string?, Task>(async _ =>
                        {
                            handlerEntered.SetResult();
                            await releaseHandler.Task;
                        }))));

            comp.Instance.CodeItems[0].Value = "7";
            var interaction = comp.InvokeAsync(() => comp.Instance.OnAfterChange(0));
            await handlerEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            interaction.IsCompleted.Should().BeFalse();

            releaseHandler.SetResult();
            await interaction;
        }

        [Test]
        public async Task SecurityCode_ShouldOnlyCompleteLatestCurrentInteractionDuringReentrantPublication()
        {
            var firstPublicationEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirstPublication = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var publishedValues = new List<string?>();
            var completedValues = new List<string?>();
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "#")
                    .Add(p => p.CodeChanged, EventCallback.Factory.Create<string?>(this,
                        new Func<string?, Task>(async value =>
                        {
                            publishedValues.Add(value);
                            if (value == "1")
                            {
                                firstPublicationEntered.TrySetResult();
                                await releaseFirstPublication.Task;
                            }
                        })))
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this,
                        value => completedValues.Add(value))));

            comp.Instance.CodeItems[0].Value = "1";
            var firstInteraction = comp.InvokeAsync(() => comp.Instance.OnAfterChange(0));
            await firstPublicationEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            comp.Instance.CodeItems[0].Value = "2";
            await comp.InvokeAsync(() => comp.Instance.OnAfterChange(0));

            releaseFirstPublication.SetResult();
            await firstInteraction;

            comp.Instance._codeState.Value.Should().Be("2");
            publishedValues.Should().Equal("1", "2");
            completedValues.Should().Equal("2");
        }

        [Test]
        public async Task SecurityCode_ShouldNotCompleteAfterDisposalDuringPublication()
        {
            var moduleMock = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            moduleMock.Setup<bool>("init", _ => true);
            moduleMock.Setup<bool>("cleanup", _ => true);
            var publicationEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releasePublication = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var completedValues = new List<string?>();
            var comp = Context.Render<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "#")
                .Add(p => p.CodeChanged, EventCallback.Factory.Create<string?>(this,
                    new Func<string?, Task>(async _ =>
                    {
                        publicationEntered.TrySetResult();
                        await releasePublication.Task;
                    })))
                .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this,
                    value => completedValues.Add(value))));

            comp.Instance.CodeItems[0].Value = "7";
            var interaction = comp.InvokeAsync(() => comp.Instance.OnAfterChange(0));
            await publicationEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await comp.InvokeAsync(() => comp.Instance.DisposeAsync().AsTask());
            releasePublication.SetResult();
            await interaction;

            completedValues.Should().BeEmpty();
        }

        [Test]
        public async Task SecurityCode_ShouldNotMoveFocusAfterDisposalDuringValidation()
        {
            var moduleMock = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            moduleMock.Setup<bool>("init", _ => true);
            var cleanup = moduleMock.SetupVoid("cleanup", _ => true);
            moduleMock.Setup<bool>("focusNextAfterContainer", _ => true);
            var validationEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseValidation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var comp = Context.Render<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "#"));
            EventHandler configureValidation = null!;
            configureValidation = (_, _) =>
            {
                comp.OnAfterRender -= configureValidation;
                comp.FindComponent<MudTextField<string>>().Render(parameters => parameters
                    .Add(p => p.Validation, new Func<string?, Task<string?>>(async _ =>
                    {
                        validationEntered.TrySetResult();
                        await releaseValidation.Task;
                        return null;
                    })));
            };
            comp.OnAfterRender += configureValidation;

            comp.Instance.CodeItems[0].Value = "7";
            var interaction = comp.InvokeAsync(() => comp.Instance.OnAfterChange(0));
            await validationEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var disposal = comp.InvokeAsync(() => comp.Instance.DisposeAsync().AsTask());
            comp.WaitForAssertion(() => cleanup.Invocations.Should().ContainSingle());
            releaseValidation.SetResult();
            await interaction;
            cleanup.SetVoidResult();
            await disposal;

            moduleMock.Invocations.Should().NotContain(invocation => invocation.Identifier == "focusNextAfterContainer");
        }

        [Test]
        public async Task SecurityCode_ShouldNotCompleteNoOpInvalidPasteIntoCompleteCode()
        {
            var completionCount = 0;
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "#")
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, _ => completionCount++)));

            await comp.InvokeAsync(() => comp.Find(".mudx-code-item input").Input("7"));
            completionCount.Should().Be(1);

            await comp.InvokeAsync(() =>
                comp.Instance.ClipboardPasteEvent("mudx-code-0-random-guid", "X"));

            comp.Instance._codeState.Value.Should().Be("7");
            completionCount.Should().Be(1);
        }

        [Test]
        public async Task SecurityCode_ShouldNotRepublishOrCompleteIdenticalValidPasteIntoCompleteCode()
        {
            var publishedValues = new List<string?>();
            var completionCount = 0;
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "#")
                    .Add(p => p.CodeChanged, EventCallback.Factory.Create<string?>(this, value => publishedValues.Add(value)))
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, _ => completionCount++)));

            await comp.InvokeAsync(() => comp.Find(".mudx-code-item input").Input("7"));
            await comp.InvokeAsync(() =>
                comp.Instance.ClipboardPasteEvent("mudx-code-0-random-guid", "7"));

            publishedValues.Should().Equal("7");
            comp.Instance._codeState.Value.Should().Be("7");
            completionCount.Should().Be(1);
        }

        [Test]
        public async Task SecurityCode_ShouldPublishAndCompleteChangedValidPasteIntoCompleteCode()
        {
            var publishedValues = new List<string?>();
            var completedValues = new List<string?>();
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "#")
                    .Add(p => p.CodeChanged, EventCallback.Factory.Create<string?>(this, value => publishedValues.Add(value)))
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, value => completedValues.Add(value))));

            await comp.InvokeAsync(() => comp.Find(".mudx-code-item input").Input("7"));
            await comp.InvokeAsync(() =>
                comp.Instance.ClipboardPasteEvent("mudx-code-0-random-guid", "8"));

            publishedValues.Should().Equal("7", "8");
            comp.Instance._codeState.Value.Should().Be("8");
            completedValues.Should().Equal("7", "8");
        }

        [Test]
        public async Task SecurityCode_ShouldValidateFormAfterPaste()
        {
            var comp = Context.Render<MudXSecurityCode>(
                parameters => parameters.Add(p => p.Pattern, "##/##"));
            var form = comp.FindComponent<MudForm>();

            await comp.InvokeAsync(() =>
                comp.Instance.ClipboardPasteEvent("mudx-code-0-random-guid", "12/34"));

            comp.Instance._codeState.Value.Should().Be("12/34");
            form.Instance.IsValid.Should().BeTrue();
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
            var comp = Context.Render<MudXSecurityCode>(
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
            var comp = Context.Render<SecurityCodeBasicTest>();
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
