#pragma warning disable
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudX.UnitTests.Viewer.TestComponents.Splitter;
using MudX.Utilities;
using NUnit.Framework;

namespace MudX.UnitTests.Components
{
    [TestFixture]
    public class SplitterTests : BunitTest
    {

        [Test]
        public void Splitter_Rendered()
        {
            var comp = Context.RenderComponent<SplitterBasicTest>();
            comp.Find("div.mudx-splitter-container").Should().NotBeNull();
            comp.Find("div.mudx-splitter-container.mudx-splitter-horizontal").Should().NotBeNull();
            comp.Find("div.mudx-splitter-container .mudx-splitter-start").Should().NotBeNull();
            comp.Find("div.mudx-splitter-container .mudx-splitter-start.mudx-splitter-horizontal").Should().NotBeNull();
            comp.Find("div.mudx-splitter-separator").Should().NotBeNull();
            comp.Find("div.mudx-splitter-separator.mudx-splitter-horizontal").Should().NotBeNull();
            comp.Find("div.mudx-splitter-separator .mudx-splitter-hr").Should().NotBeNull();
            comp.Find("div.mudx-splitter-container .mudx-splitter-end").Should().NotBeNull();
            comp.Find("div.mudx-splitter-container .mudx-splitter-end.mudx-splitter-horizontal").Should().NotBeNull();
        }

        [Test]
        public async Task Splitter_Tests_JSModule()
        {
            // Arrange: Setup JSInterop to expect the import and initialize calls
            var jsInterop = Context.JSInterop;

            // Setup the import call to return a mock module
            var moduleMock = jsInterop.SetupModule(AssemblyInfo.ModulePath("mudxSplitter.js"));
            // Setup startDrag to return an array [100, 100]
            moduleMock
                .Setup<double[]>("startDrag", _ => true) // match any arguments
                .SetResult([100, 100]);

            moduleMock.Setup<bool>("cancelDrag", _ => true);

            var comp = Context.RenderComponent<SplitterBasicTest>();
            var splitter = comp.FindComponent<MudXSplitter>();
            splitter.Should().NotBeNull();

            // Assert: Verify the JS module was imported
            jsInterop.VerifyInvoke("import")
                .Arguments[0].Should().Be(AssemblyInfo.ModulePath("mudxSplitter.js"));
            splitter.Instance._module.Should().NotBeNull();

            // Assert: Start Drag Process
            PointerEventArgs pointerEventArgs = new()
            {
                PointerId = 1,
                ClientX = 100,
                ClientY = 100
            };

            await comp.InvokeAsync(async () => await splitter.Instance.SetDraggingState(true, pointerEventArgs));
            splitter.Instance._viewportSize!.Value.Width.Should().Be(100);
            splitter.Instance._viewportSize!.Value.Height.Should().Be(100);

            moduleMock.VerifyInvoke("startDrag"); // 1 time
            moduleMock.VerifyNotInvoke("cancelDrag"); // 0 times
            // Assert: Stop Drag Process
            await comp.InvokeAsync(async () => await splitter.Instance.SetDraggingState(false, pointerEventArgs));

            moduleMock.VerifyInvoke("startDrag"); // still 1 time
            moduleMock.VerifyInvoke("cancelDrag"); // 1 time

            // dispose the component
            await splitter.Instance.DisposeAsync();
            comp.WaitForAssertion(() => splitter.Instance._module.Should().BeNull());
            splitter.Instance._disposing.Should().BeTrue();
        }

        [Test]
        public void Constructor_ShouldSetDefaults()
        {
            var comp = Context.RenderComponent<MudXSplitter>(p => p
                .Add(x => x.StartSplitter, FragmentMock)
                .Add(x => x.EndSplitter, FragmentMock)
            );
            var splitter = comp.Instance;
            splitter.StartSize.Should().Be(50);
            splitter.Direction.Should().Be(SplitterDirection.Horizontal);
            splitter.Height.Should().Be("100%");
            splitter.Width.Should().Be("100%");
            splitter.StartMinimumSize.Should().Be(0);
            splitter.EndMinimumSize.Should().Be(0);
        }

        [Test]
        public void Splitter_ShouldReturnExpectedClasses_Horizontal()
        {
            var comp = Context.RenderComponent<MudXSplitter>(p => p
                .Add(x => x.StartSplitter, FragmentMock)
                .Add(x => x.EndSplitter, FragmentMock)
            );
            var splitter = comp.Instance;
            var type = splitter.GetType();

            string GetProtectedProperty(string name)
                => type.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(splitter) as string ?? string.Empty;

            var className = GetProtectedProperty("SeparatorContainerClassname");
            className.Should().Contain("mudx-splitter-container");
            className.Should().Contain("mudx-splitter-horizontal");

            className = GetProtectedProperty("SeparatorClassname");
            className.Should().Contain("mudx-splitter-separator");
            className.Should().Contain("mudx-splitter-horizontal");

            className = GetProtectedProperty("StartSplitterClassname");
            className.Should().Contain("mudx-splitter-start");
            className.Should().Contain("mudx-splitter-horizontal");

            className = GetProtectedProperty("EndSplitterClassname");
            className.Should().Contain("mudx-splitter-end");
            className.Should().Contain("mudx-splitter-horizontal");
        }

        [Test]
        public void Splitter_ShouldReturnExpectedClasses_Vertical()
        {
            var comp = Context.RenderComponent<MudXSplitter>(p => p
                .Add(x => x.StartSplitter, FragmentMock)
                .Add(x => x.EndSplitter, FragmentMock)
                .Add(x => x.Direction, SplitterDirection.Vertical)
            );
            var splitter = comp.Instance;
            var type = splitter.GetType();

            string GetProtectedProperty(string name)
                => type.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(splitter) as string ?? string.Empty;

            var className = GetProtectedProperty("SeparatorContainerClassname");
            className.Should().Contain("mudx-splitter-container");
            className.Should().Contain("mudx-splitter-vertical");

            className = GetProtectedProperty("SeparatorClassname");
            className.Should().Contain("mudx-splitter-separator");
            className.Should().Contain("mudx-splitter-vertical");

            className = GetProtectedProperty("StartSplitterClassname");
            className.Should().Contain("mudx-splitter-start");
            className.Should().Contain("mudx-splitter-vertical");

            className = GetProtectedProperty("EndSplitterClassname");
            className.Should().Contain("mudx-splitter-end");
            className.Should().Contain("mudx-splitter-vertical");
        }

        [Test]
        public void Splitter_ShouldReturnExpectedStyles()
        {
            var comp = Context.RenderComponent<MudXSplitter>(p => p
                 .Add(x => x.StartSplitter, FragmentMock)
                 .Add(x => x.EndSplitter, FragmentMock)
             );
            var splitter = comp.Instance;
            var type = splitter.GetType();

            string GetProtectedProperty(string name)
                => type.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(splitter) as string ?? string.Empty;

            var style = GetProtectedProperty("SeparatorContainerStylename");
            style.Should().Contain($"height:{splitter.Height}");
            style.Should().Contain($"width:{splitter.Width}");
            style.Should().Contain($"grid-template-columns:{splitter._startSizeState.Value}% auto 1fr");

            comp.SetParametersAndRender(p => p.Add(x => x.Direction, SplitterDirection.Vertical));
            style = GetProtectedProperty("SeparatorContainerStylename");
            style.Should().Contain($"grid-template-rows:{splitter._startSizeState.Value}% auto 1fr");
        }

        [Test]
        public void Splitter_ShouldRenderCustomSeparatorTemplate()
        {
            // Arrange: Render with a custom SeparatorTemplate
            var customSeparator = (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "custom-separator");
                builder.AddContent(2, "Custom Separator");
                builder.CloseElement();
            });

            var comp = Context.RenderComponent<MudXSplitter>(p => p
                .Add(x => x.StartSplitter, FragmentMock)
                .Add(x => x.EndSplitter, FragmentMock)
                .Add(x => x.SeparatorTemplate, customSeparator)
            );

            // Act & Assert: Should render the custom separator and not the default
            var separatorDiv = comp.Find("div.mudx-splitter-separator .custom-separator");
            separatorDiv.Should().NotBeNull();
            separatorDiv.TextContent.Should().Be("Custom Separator");
            comp.Markup.Should().NotContain("mudx-splitter-hr");
        }

        [Test]
        public async Task PerformPointerDrag_ShouldUpdateStartSize_Vertical()
        {
            var jsInterop = Context.JSInterop;
            var moduleMock = jsInterop.SetupModule(AssemblyInfo.ModulePath("mudxSplitter.js"));
            // Setup startDrag to return an array [100, 100]
            moduleMock
                .Setup<double[]>("startDrag", _ => true)
                .SetResult([100, 100]);

            var comp = Context.RenderComponent<SplitterSizingTest>(p => p.Add(x => x.SplitterDirection, SplitterDirection.Vertical));
            var splitter = comp.FindComponent<MudXSplitter>();

            var args = new PointerEventArgs
            {
                PointerId = 1,
                ClientX = 0,
                ClientY = 33
            };

            splitter.Instance.StartSize.Should().Be(comp.Instance.StartSize);
            splitter.Instance._dragging.Should().BeFalse();
            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerDown(args)
            );
            splitter.Instance._dragging.Should().BeTrue();

            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerCancel(args)
            );
            splitter.Instance._dragging.Should().BeFalse();

            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerDown(args)
            );
            splitter.Instance._dragging.Should().BeTrue();

            args = new PointerEventArgs
            {
                PointerId = 1,
                ClientX = 0,
                ClientY = 70
            };

            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerMove(args)
            );
            splitter.Instance.StartSize.Should().Be(70);

            await Task.Delay(splitter.Instance.PointerMoveThrottle);
            args = new PointerEventArgs
            {
                PointerId = 1,
                ClientX = 0,
                ClientY = 5 // beyond start min size
            };

            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerMove(args)
            );
            comp.WaitForAssertion(() =>
                splitter.Instance._startSizeState.Value.Should().Be(splitter.Instance.StartMinimumSize)
            );

            await Task.Delay(splitter.Instance.PointerMoveThrottle);
            args = new PointerEventArgs
            {
                PointerId = 1,
                ClientX = 0,
                ClientY = 95 // beyond end min size
            };

            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerMove(args)
            );
            comp.WaitForAssertion(() =>
                splitter.Instance._startSizeState.Value.Should().Be(100 - splitter.Instance.EndMinimumSize)
            );

            await Task.Delay(splitter.Instance.PointerMoveThrottle);
            args = new PointerEventArgs
            {
                PointerId = 1,
                ClientX = 0,
                ClientY = 66
            };

            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerUp(args)
            );
            splitter.Instance.StartSize.Should().Be(66);
        }

        [Test]
        public async Task PerformPointerDrag_ShouldUpdateStartSize_Horizontal()
        {
            var jsInterop = Context.JSInterop;
            var moduleMock = jsInterop.SetupModule(AssemblyInfo.ModulePath("mudxSplitter.js"));
            // Setup startDrag to return an array [100, 100]
            moduleMock
                .Setup<double[]>("startDrag", _ => true) // match any arguments
                .SetResult([100, 100]);

            var comp = Context.RenderComponent<SplitterSizingTest>(p => p.Add(x => x.SplitterDirection, SplitterDirection.Horizontal));
            var splitter = comp.FindComponent<MudXSplitter>();

            var args = new PointerEventArgs
            {
                PointerId = 1,
                ClientX = 33,
                ClientY = 0
            };

            splitter.Instance.StartSize.Should().Be(comp.Instance.StartSize);
            splitter.Instance._dragging.Should().BeFalse();
            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerDown(args)
            );
            splitter.Instance._dragging.Should().BeTrue();

            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerCancel(args)
            );
            splitter.Instance._dragging.Should().BeFalse();

            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerDown(args)
            );
            splitter.Instance._dragging.Should().BeTrue();

            args = new PointerEventArgs
            {
                PointerId = 1,
                ClientX = 70,
                ClientY = 0
            };

            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerMove(args)
            );
            splitter.Instance.StartSize.Should().Be(70);

            await Task.Delay(splitter.Instance.PointerMoveThrottle);
            args = new PointerEventArgs
            {
                PointerId = 1,
                ClientX = 5, // beyond start min size
                ClientY = 0
            };

            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerMove(args)
            );
            comp.WaitForAssertion(() =>
                splitter.Instance._startSizeState.Value.Should().Be(splitter.Instance.StartMinimumSize)
            ); // should clamp to StartMinimumSize

            await Task.Delay(splitter.Instance.PointerMoveThrottle);
            args = new PointerEventArgs
            {
                PointerId = 1,
                ClientX = 95, // beyond end min size
                ClientY = 0
            };

            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerMove(args)
            );
            comp.WaitForAssertion(() =>
                splitter.Instance._startSizeState.Value.Should().Be(100 - splitter.Instance.EndMinimumSize)
            );

            await Task.Delay(splitter.Instance.PointerMoveThrottle);
            args = new PointerEventArgs
            {
                PointerId = 1,
                ClientX = 66,
                ClientY = 0
            };

            await comp.InvokeAsync(async () =>
                await splitter.Instance.OnPointerUp(args)
            );
            splitter.Instance.StartSize.Should().Be(66);
        }

        [Test]
        public void OnStartSizeChanged_ShouldClampValue()
        {
            var comp = Context.RenderComponent<MudXSplitter>(p => p
                 .Add(x => x.StartSplitter, FragmentMock)
                 .Add(x => x.StartMinimumSize, 10)
                 .Add(x => x.EndMinimumSize, 10)
                 .Add(x => x.EndSplitter, FragmentMock)
             );
            var splitter = comp.Instance;

            comp.SetParametersAndRender(p => p.Add(x => x.StartSize, 5));

            comp.WaitForAssertion(() => splitter._startSizeState.Value.Should().BeGreaterThanOrEqualTo(10));

            comp.SetParametersAndRender(p => p.Add(x => x.StartSize, 95));

            comp.WaitForAssertion(() => splitter._startSizeState.Value.Should().BeLessThanOrEqualTo(90)); // 100 - EndMinimumSize
        }

        private RenderFragment FragmentMock => builder =>
        {
            builder.AddContent(0, "Mock Content");
        };
    }
}
