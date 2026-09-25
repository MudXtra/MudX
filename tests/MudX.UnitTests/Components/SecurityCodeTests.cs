using System.Reflection;
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

            var comp = Context.RenderComponent<SecurityCodeBasicTest>();
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

        [Test]
        public async Task SecurityCode_ShouldValidateFormAfterTerminalInput()
        {
            var comp = Context.RenderComponent<MudXSecurityCode>(
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
            var comp = Context.RenderComponent<MudXSecurityCode>(
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
            var comp = Context.RenderComponent<MudXSecurityCode>(
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
            var comp = Context.RenderComponent<MudXSecurityCode>(
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
            var comp = Context.RenderComponent<MudXSecurityCode>(
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
            var comp = Context.RenderComponent<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "##/##")
                    .Add(p => p.CodeChanged, EventCallback.Factory.Create<string?>(this, value => eventOrder.Add($"published:{value}")))
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, value =>
                    {
                        eventOrder.Add($"completed:{value}");
                        form?.IsValid.Should().BeTrue();
                    })));
            form = comp.FindComponent<MudForm>().Instance;

            await comp.InvokeAsync(() => comp.Instance.ClipboardPasteEvent(comp.Instance.CodeItems[0].InputId, "12/34"));

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
            var comp = Context.RenderComponent<MudXSecurityCode>(
                parameters => parameters.Add(p => p.Pattern, "##/##"));
            var form = comp.FindComponent<MudForm>();

            await comp.InvokeAsync(() =>
                comp.Instance.ClipboardPasteEvent(comp.Instance.CodeItems[0].InputId, "12/34"));

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
            var comp = Context.RenderComponent<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "####")
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, _ => completionCount++)));

            await comp.InvokeAsync(() => comp.Instance.ClipboardPasteEvent(comp.Instance.CodeItems[0].InputId, "12"));

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
            var comp = Context.RenderComponent<MudXSecurityCode>(
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
            var comp = Context.RenderComponent<MudXSecurityCode>(
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
            var comp = Context.RenderComponent<MudXSecurityCode>(
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
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
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
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "#"));
            EventHandler configureValidation = null!;
            configureValidation = (_, _) =>
            {
                comp.OnAfterRender -= configureValidation;
                comp.FindComponent<MudTextField<string>>().SetParametersAndRender(parameters => parameters
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
            var comp = Context.RenderComponent<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "#")
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, _ => completionCount++)));

            await comp.InvokeAsync(() => comp.Find(".mudx-code-item input").Input("7"));
            completionCount.Should().Be(1);

            await comp.InvokeAsync(() =>
                comp.Instance.ClipboardPasteEvent(comp.Instance.CodeItems[0].InputId, "X"));

            comp.Instance._codeState.Value.Should().Be("7");
            completionCount.Should().Be(1);
        }

        [Test]
        public async Task SecurityCode_ShouldNotRepublishOrCompleteIdenticalValidPasteIntoCompleteCode()
        {
            var publishedValues = new List<string?>();
            var completionCount = 0;
            var comp = Context.RenderComponent<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "#")
                    .Add(p => p.CodeChanged, EventCallback.Factory.Create<string?>(this, value => publishedValues.Add(value)))
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, _ => completionCount++)));

            await comp.InvokeAsync(() => comp.Find(".mudx-code-item input").Input("7"));
            await comp.InvokeAsync(() =>
                comp.Instance.ClipboardPasteEvent(comp.Instance.CodeItems[0].InputId, "7"));

            publishedValues.Should().Equal("7");
            comp.Instance._codeState.Value.Should().Be("7");
            completionCount.Should().Be(1);
        }

        [Test]
        public async Task SecurityCode_ShouldPublishAndCompleteChangedValidPasteIntoCompleteCode()
        {
            var publishedValues = new List<string?>();
            var completedValues = new List<string?>();
            var comp = Context.RenderComponent<MudXSecurityCode>(
                parameters => parameters
                    .Add(p => p.Pattern, "#")
                    .Add(p => p.CodeChanged, EventCallback.Factory.Create<string?>(this, value => publishedValues.Add(value)))
                    .Add(p => p.OnCompleted, EventCallback.Factory.Create<string?>(this, value => completedValues.Add(value))));

            await comp.InvokeAsync(() => comp.Find(".mudx-code-item input").Input("7"));
            await comp.InvokeAsync(() =>
                comp.Instance.ClipboardPasteEvent(comp.Instance.CodeItems[0].InputId, "8"));

            publishedValues.Should().Equal("7", "8");
            comp.Instance._codeState.Value.Should().Be("8");
            completedValues.Should().Equal("7", "8");
        }

        [Test]
        public async Task SecurityCode_ShouldValidateFormAfterPaste()
        {
            var comp = Context.RenderComponent<MudXSecurityCode>(
                parameters => parameters.Add(p => p.Pattern, "##/##"));
            var form = comp.FindComponent<MudForm>();

            await comp.InvokeAsync(() =>
                comp.Instance.ClipboardPasteEvent(comp.Instance.CodeItems[0].InputId, "12/34"));

            comp.Instance._codeState.Value.Should().Be("12/34");
            form.Instance.IsValid.Should().BeTrue();
        }

        [Test]
        public async Task SecurityCode_ShouldPublishOnceAndIncludeAllTrailingLiterals()
        {
            var publishedValues = new List<string?>();
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "##/-")
                .Add(p => p.CodeChanged, value => publishedValues.Add(value)));

            await comp.InvokeAsync(() => comp.Instance.ClipboardPasteEvent(comp.Instance.CodeItems[0].InputId, "12"));

            comp.Instance._codeState.Value.Should().Be("12/-");
            publishedValues.Should().Equal("12/-");
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

        [TestCase(false)]
        [TestCase(true)]
        public async Task SecurityCode_ShouldUpdateSegmentAriaLabelWhenUserAttributesChange(bool mutateInPlace)
        {
            var attributes = new Dictionary<string, object?> { ["aria-label"] = "Initial segment" };
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.AriaLabel, "Account verification code")
                .Add(p => p.UserAttributes, attributes));
            var inputIds = comp.FindAll("input").Select(input => input.Id).ToArray();
            comp.FindAll("input:not([readonly])")
                .Should().OnlyContain(input => input.GetAttribute("aria-label") == "Initial segment");

            await comp.InvokeAsync(() =>
            {
                if (mutateInPlace)
                    attributes["aria-label"] = "Updated segment";
                else
                    attributes = new Dictionary<string, object?> { ["aria-label"] = "Updated segment" };
                comp.SetParametersAndRender(parameters => parameters.Add(p => p.UserAttributes, attributes));
            });

            comp.FindAll("input:not([readonly])")
                .Should().OnlyContain(input => input.GetAttribute("aria-label") == "Updated segment");
            comp.FindAll("input").Select(input => input.Id).Should().Equal(inputIds);
            comp.Find(".mudx-code-container[role='group']")
                .GetAttribute("aria-label").Should().Be("Account verification code");
            attributes.Should().ContainSingle().Which.Value.Should().Be("Updated segment");
        }

        [TestCase("remove")]
        [TestCase("clear")]
        [TestCase("replace")]
        [TestCase("null-label")]
        [TestCase("null-dictionary")]
        public async Task SecurityCode_ShouldRestoreOrdinalLabelsWhenUserAttributesAreRemoved(string removal)
        {
            var attributes = new Dictionary<string, object?> { ["aria-label"] = "Initial segment" };
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.UserAttributes, attributes));
            comp.FindAll("input:not([readonly])")
                .Should().OnlyContain(input => input.GetAttribute("aria-label") == "Initial segment");

            await comp.InvokeAsync(() =>
            {
                switch (removal)
                {
                    case "remove":
                        attributes.Remove("aria-label");
                        break;
                    case "clear":
                        attributes.Clear();
                        break;
                    case "replace":
                        attributes = new Dictionary<string, object?>();
                        break;
                    case "null-label":
                        attributes["aria-label"] = null;
                        break;
                    case "null-dictionary":
                        attributes = null!;
                        break;
                }
                comp.SetParametersAndRender(parameters => parameters.Add(p => p.UserAttributes, attributes));
            });

            comp.FindAll("input:not([readonly])")
                .Select(input => input.GetAttribute("aria-label"))
                .Should().Equal("Character 1 of 4", "Character 2 of 4", "Character 3 of 4", "Character 4 of 4");
            comp.FindAll("input").Should().OnlyContain(input => input.GetAttribute("autocomplete") == "off");
        }

        [Test]
        public async Task SecurityCode_ShouldUpdateAutocompleteWithoutOverridingLiteralOrFormState()
        {
            var attributes = new Dictionary<string, object?>
            {
                ["autocomplete"] = "one-time-code",
                ["tabindex"] = "3",
                ["aria-hidden"] = "false"
            };
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "##-##")
                .Add(p => p.Required, true)
                .Add(p => p.Disabled, true)
                .Add(p => p.Error, true)
                .Add(p => p.HelperText, "Enter the code.")
                .Add(p => p.ErrorText, "Invalid code.")
                .Add(p => p.UserAttributes, attributes));
            var inputIds = comp.FindAll("input").Select(input => input.Id).ToArray();
            comp.FindAll("input").Should().OnlyContain(input => input.GetAttribute("autocomplete") == "one-time-code");

            await comp.InvokeAsync(() =>
            {
                attributes["autocomplete"] = "on";
                attributes["tabindex"] = "4";
                comp.SetParametersAndRender(parameters => parameters.Add(p => p.UserAttributes, attributes));
            });

            comp.FindAll("input").Should().OnlyContain(input => input.GetAttribute("autocomplete") == "on");
            comp.FindAll("input:not([readonly])").Should().OnlyContain(input =>
                input.GetAttribute("tabindex") == "4"
                && input.GetAttribute("aria-hidden") == "false"
                && input.GetAttribute("aria-required") == "true"
                && input.GetAttribute("aria-invalid") == "true"
                && input.HasAttribute("disabled"));
            foreach (var input in comp.FindAll("input:not([readonly])"))
            {
                input.GetAttribute("aria-describedby")!.Split(' ')
                    .Select(id => comp.Find($"#{id}").TextContent)
                    .Should().BeEquivalentTo("Enter the code.", "Invalid code.");
            }
            comp.Find("input[readonly]").GetAttribute("tabindex").Should().Be("-1");
            comp.Find("input[readonly]").GetAttribute("aria-hidden").Should().Be("true");
            comp.Find("input[readonly]").HasAttribute("inert").Should().BeTrue();
            comp.Find("input[readonly]").HasAttribute("disabled").Should().BeFalse();
            comp.FindAll("[role='alert']").Should().ContainSingle();

            await comp.InvokeAsync(() =>
            {
                attributes.Clear();
                comp.SetParametersAndRender(parameters => parameters.Add(p => p.UserAttributes, attributes));
            });

            comp.FindAll("input").Should().OnlyContain(input => input.GetAttribute("autocomplete") == "off");
            comp.FindAll("input:not([readonly])").Should().OnlyContain(input =>
                !input.HasAttribute("tabindex") && !input.HasAttribute("aria-hidden"));
            comp.FindAll("input:not([readonly])").Select(input => input.GetAttribute("aria-label"))
                .Should().Equal("Character 1 of 4", "Character 2 of 4", "Character 3 of 4", "Character 4 of 4");
            comp.Find("input[readonly]").GetAttribute("tabindex").Should().Be("-1");
            comp.Find("input[readonly]").GetAttribute("aria-hidden").Should().Be("true");
            comp.Find("input[readonly]").HasAttribute("inert").Should().BeTrue();
            comp.FindAll("input").Select(input => input.Id).Should().Equal(inputIds);
            attributes.Should().BeEmpty();
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
                .Select(id => comp.Find($"[id='{id}']").TextContent)
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

        [TestCase("typing", "Invalid code.")]
        [TestCase("paste", "Invalid code.")]
        [TestCase("validate", "Invalid code.")]
        [TestCase("typing", null)]
        [TestCase("paste", null)]
        [TestCase("validate", null)]
        public async Task SecurityCode_ShouldRetainExternalErrorThroughValidation(string interaction, string? errorText)
        {
            var completed = new List<string?>();
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "##-##")
                .Add(p => p.Error, true)
                .Add(p => p.ErrorText, errorText)
                .Add(p => p.HelperText, "Enter the code.")
                .Add(p => p.OnCompleted, value => completed.Add(value)));

            comp.FindAll("input:not([readonly])").Should().OnlyContain(input => input.GetAttribute("aria-invalid") == "true");
            switch (interaction)
            {
                case "typing":
                    await comp.Find("input:not([readonly])").InputAsync(new ChangeEventArgs { Value = "1" });
                    break;
                case "paste":
                    await comp.InvokeAsync(() => comp.Instance.ClipboardPasteEvent(comp.Find("input:not([readonly])").Id!, "1234"));
                    break;
                case "validate":
                    await comp.InvokeAsync(() => comp.FindComponent<MudForm>().Instance.ValidateAsync());
                    break;
            }

            comp.Instance.Error.Should().BeTrue();
            comp.FindAll("input:not([readonly])").Should().OnlyContain(input =>
                input.GetAttribute("aria-invalid") == "true" && input.Closest(".mud-input")!.ClassList.Contains("mud-input-error"));
            foreach (var input in comp.FindAll("input:not([readonly])"))
            {
                var descriptions = input.GetAttribute("aria-describedby")!.Split(' ').Select(id => comp.Find($"[id='{id}']").TextContent).ToArray();
                descriptions.Should().Contain("Enter the code.");
                if (errorText is not null)
                    descriptions.Should().Contain(errorText);
            }
            comp.FindAll("[role='alert']").Should().HaveCount(errorText is null ? 0 : 1);
            comp.FindAll(".mudx-code-item .mud-input-control-helper-container").Should().OnlyContain(element => string.IsNullOrWhiteSpace(element.TextContent));
            comp.Find("input[readonly]").GetAttribute("aria-invalid").Should().Be("false");
            completed.Should().BeEmpty("completion requires the internal form to pass validation");
        }

        [Test]
        public async Task SecurityCode_ShouldClearExternalErrorWithoutAnotherEdit()
        {
            var completed = new List<string?>();
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "##")
                .Add(p => p.Error, true)
                .Add(p => p.ErrorText, "Invalid code.")
                .Add(p => p.HelperText, "Enter the code.")
                .Add(p => p.OnCompleted, value => completed.Add(value)));
            await comp.InvokeAsync(() => comp.Instance.ClipboardPasteEvent(comp.Find("input").Id!, "12"));

            await comp.InvokeAsync(() => comp.SetParametersAndRender(parameters => parameters.Add(p => p.Error, false)));

            comp.FindAll("input").Should().OnlyContain(input => input.GetAttribute("aria-invalid") == "false");
            comp.FindAll(".mud-input-error").Should().BeEmpty();
            comp.FindAll("[role='alert']").Should().BeEmpty();
            foreach (var input in comp.FindAll("input"))
                input.GetAttribute("aria-describedby").Should().Be(comp.Find(".mudx-code-helper-text").Id);
            comp.FindComponent<MudForm>().Instance.IsValid.Should().BeTrue();
            completed.Should().BeEmpty("a parameter update must not manufacture a completion");

            await comp.FindAll("input")[1].InputAsync(new ChangeEventArgs { Value = "3" });
            completed.Should().Equal("13");
        }

        [Test]
        public async Task SecurityCode_ShouldKeepCharacterValidationAfterExternalErrorClears()
        {
            var completed = new List<string?>();
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "#")
                .Add(p => p.Required, true)
                .Add(p => p.Error, true)
                .Add(p => p.ErrorText, "Invalid code.")
                .Add(p => p.OnCompleted, value => completed.Add(value)));
            await comp.Find("input").InputAsync(new ChangeEventArgs { Value = "x" });
            await comp.InvokeAsync(() => comp.SetParametersAndRender(parameters => parameters.Add(p => p.Error, false)));

            comp.Instance.CodeItems[0].Value.Should().BeEmpty();
            comp.Find("input").GetAttribute("aria-invalid").Should().Be("true");
            comp.FindComponent<MudForm>().Instance.IsValid.Should().BeFalse();
            comp.FindAll("[role='alert']").Should().BeEmpty();
            completed.Should().BeEmpty();

            await comp.Find("input").InputAsync(new ChangeEventArgs { Value = "7" });
            comp.Find("input").GetAttribute("aria-invalid").Should().Be("false");
            comp.FindComponent<MudForm>().Instance.IsValid.Should().BeTrue();
            completed.Should().Equal("7");
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
            await comp.InvokeAsync(async () => await comp.Instance.ClipboardPasteEvent(comp.Instance.CodeItems[0].InputId, pasteText));
            comp.WaitForAssertion(() => comp.Instance._codeState.Value.Should().Be(expectedValue));
            comp.Instance.CodeItems[0].Value.Should().Be(expectedValue[..1]);

            // reset value and Items (ensure onchangehandler happens)
            await comp.InvokeAsync(async () => await comp.Instance._codeState.SetValueAsync(default));
            comp.WaitForAssertion(() => comp.Instance._codeState.Value.Should().Be(null));
            comp.Instance.CodeItems[0].Value = string.Empty; // make sure items are reset

            // start paste at position 1
            await comp.InvokeAsync(async () => await comp.Instance.ClipboardPasteEvent(comp.Instance.CodeItems[1].InputId, pasteText));
            comp.WaitForAssertion(() => comp.Instance._codeState.Value.Should().Be(expectedValue2));
            comp.Instance.CodeItems[1].Value.Should().Be(expectedValue[..1]);
        }

        /// <summary>
        /// The paste bridge accepts only an exact editable input identifier owned by this component.
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("xxxxxxxxxx0-foreign")]
        [TestCase("mudX-code-0-")]
        [TestCase("mudX-code--1-foreign")]
        [TestCase("mudX-code-999-foreign")]
        [TestCase("mudX-code-0-foreign-extra")]
        public async Task SecurityCode_PasteBridge_RejectsMalformedIds(string? invalidId)
        {
            var notifications = new List<string?>();
            var validationCalls = 0;
            var module = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            module.Setup<bool>("init", _ => true);
            module.Setup<bool>("focusBlock", _ => true);
            module.Setup<bool>("focusNextAfterContainer", _ => true);
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(x => x.CodeChanged, value => notifications.Add(value)));
            SetValues(comp.Instance, (0, "1"));
            typeof(MudFormComponent<string, string>)
                .GetProperty(nameof(MudFormComponent<string, string>.Validation))!
                .SetValue(comp.Instance.CodeItems[0].TextFieldRef, new Func<string, string?>(_ =>
                {
                    validationCalls++;
                    return null;
                }));

            await comp.InvokeAsync(() => comp.Instance.ClipboardPasteEvent(invalidId!, "9"));

            comp.Instance.CodeItems.Select(x => x.Value).Should().Equal("1", "", "", "");
            comp.Instance._codeState.Value.Should().BeNull();
            notifications.Should().BeEmpty();
            validationCalls.Should().Be(0);
            module.VerifyNotInvoke("focusBlock");
            module.VerifyNotInvoke("focusNextAfterContainer");
        }

        /// <summary>
        /// A real paste input identifier from another component cannot cross the bridge boundary.
        /// </summary>
        [Test]
        public async Task SecurityCode_PasteBridge_RejectsAnotherComponentInputId()
        {
            var notifications = new List<string?>();
            var validationCalls = 0;
            var module = Context.JSInterop.SetupModule(AssemblyInfo.ModulePath("mudxSecurityCode.js"));
            module.Setup<bool>("init", _ => true);
            module.Setup<bool>("focusBlock", _ => true);
            module.Setup<bool>("focusNextAfterContainer", _ => true);
            var target = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(x => x.CodeChanged, value => notifications.Add(value)));
            var other = Context.RenderComponent<MudXSecurityCode>();
            SetValues(target.Instance, (0, "1"));
            typeof(MudFormComponent<string, string>)
                .GetProperty(nameof(MudFormComponent<string, string>.Validation))!
                .SetValue(target.Instance.CodeItems[0].TextFieldRef, new Func<string, string?>(_ =>
                {
                    validationCalls++;
                    return null;
                }));

            await target.InvokeAsync(() => target.Instance.ClipboardPasteEvent(other.Instance.CodeItems[0].InputId, "9"));

            target.Instance.CodeItems.Select(x => x.Value).Should().Equal("1", "", "", "");
            target.Instance._codeState.Value.Should().BeNull();
            notifications.Should().BeEmpty();
            validationCalls.Should().Be(0);
            module.VerifyNotInvoke("focusBlock");
            module.VerifyNotInvoke("focusNextAfterContainer");
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

        [TestCase(false)]
        [TestCase(true)]
        public async Task SecurityCode_BridgeDoesNotMutateWhenDisabledOrDisposed(bool disposed)
        {
            var published = new List<string?>();
            var completed = new List<string?>();
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "##")
                .Add(p => p.Disabled, !disposed)
                .Add(p => p.CodeChanged, value => published.Add(value))
                .Add(p => p.OnCompleted, value => completed.Add(value)));
            var id = comp.Instance.CodeItems[0].InputId;
            if (disposed)
                await comp.InvokeAsync(() => comp.Instance.DisposeAsync().AsTask());

            await comp.InvokeAsync(() => comp.Instance.ClipboardPasteEvent(id, "12"));
            var target = await comp.InvokeAsync(() => InvokeKeyboardEvent(comp.Instance, id, "Delete"));

            target.Should().BeNull();
            comp.Instance.CodeItems.Select(item => item.Value).Should().Equal("", "");
            published.Should().BeEmpty();
            completed.Should().BeEmpty();
        }

        [Test]
        public async Task SecurityCode_CompletionDoesNotRepeatForUnchangedInput()
        {
            var completed = new List<string?>();
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "#")
                .Add(p => p.OnCompleted, value => completed.Add(value)));
            await comp.InvokeAsync(() => comp.Instance.ClipboardPasteEvent(comp.Instance.CodeItems[0].InputId, "7"));

            await comp.InvokeAsync(() => comp.Instance.OnAfterChange(0));

            completed.Should().Equal("7");
        }

        [Test]
        public async Task SecurityCode_DeletionDuringPublicationCancelsStaleCompletion()
        {
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var completed = new List<string?>();
            var comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "#")
                .Add(p => p.CodeChanged, EventCallback.Factory.Create<string?>(this, new Func<string?, Task>(async value =>
                {
                    if (value == "7")
                    {
                        entered.TrySetResult();
                        await release.Task;
                    }
                })))
                .Add(p => p.OnCompleted, value => completed.Add(value)));
            var id = comp.Instance.CodeItems[0].InputId;
            var paste = comp.InvokeAsync(() => comp.Instance.ClipboardPasteEvent(id, "7"));
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await comp.InvokeAsync(() => InvokeKeyboardEvent(comp.Instance, id, "Delete"));
            release.SetResult();
            await paste;

            completed.Should().BeEmpty();
            comp.Instance._codeState.Value.Should().BeEmpty();
        }

        [Test]
        public async Task SecurityCode_ReentrantUnchangedInputDoesNotCancelCompletion()
        {
            var completed = new List<string?>();
            IRenderedComponent<MudXSecurityCode>? comp = null;
            comp = Context.RenderComponent<MudXSecurityCode>(parameters => parameters
                .Add(p => p.Pattern, "#")
                .Add(p => p.CodeChanged, EventCallback.Factory.Create<string?>(this,
                    new Func<string?, Task>(_ => comp!.Instance.OnAfterChange(0))))
                .Add(p => p.OnCompleted, value => completed.Add(value)));

            await comp.InvokeAsync(() => comp.Instance.ClipboardPasteEvent(comp.Instance.CodeItems[0].InputId, "7"));

            completed.Should().Equal("7");
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
