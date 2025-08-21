using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Utilities;
using MudX.Extensions;
using MudX.Utilities;

namespace MudX
{
    /// <summary>
    /// A splitter component for allowing users to resize two adjacent panels. Can be oriented horizontally or vertically and nested within each other.
    /// </summary>
    public partial class MudXSplitter : ComponentBase, IAsyncDisposable
    {
        internal record struct DragPoints(double XDown, double YDown, int StartSize);
        internal DragPoints? _points;
        internal DateTime _lastPointerMove = DateTime.MinValue;
        internal readonly TimeSpan PointerMoveThrottle = TimeSpan.FromMilliseconds(16);
        private bool _dragging = false;
        private ElementReference _dragElement = default!;
        private int _startSize = 50; // left or top size

        internal record struct ViewPortSize(double Width, double Height);
        internal ViewPortSize? _viewportSize;

        private IJSObjectReference? _module;
        private bool _disposing;

        /// <summary>
        /// The primary classes used on the outermost container div.
        /// </summary>
        protected string SeparatorContainerClassname =>
            new CssBuilder("mudx-splitter-container")
            .AddClass($"mudx-splitter-{Direction.ToDescription()}")
            .AddClass(Class)
            .Build();

        /// <summary>
        /// The primary class used on the div surrounding the separator and/or separator template.
        /// </summary>
        protected string SeparatorClassname =>
            new CssBuilder("mudx-splitter-separator")
            .AddClass($"mudx-splitter-{Direction.ToDescription()}")
            .Build();

        /// <summary>
        /// The style used for the splitter container.
        /// </summary>
        protected string SeparatorContainerStylename =>
            new StyleBuilder()
            .AddStyle("height", $"{Height}")
            .AddStyle("width", $"{Width}")
            .AddStyle(Style)
            .Build();

        /// <summary>
        /// Gets the style name for the start splitter, based on the current size and direction.
        /// </summary>
        protected string StartSplitterStylename =>
            new StyleBuilder()
            .AddStyle("width", $"{_startSize}%", Direction is SplitterDirection.Horizontal)
            .AddStyle("height", $"{_startSize}%", Direction is SplitterDirection.Vertical)
            .Build();

        [Inject]
        private IJSRuntime _jSRuntime { get; set; } = default!;

        /// <summary>
        /// Sets the CSS class for the outermost container div.
        /// </summary>
        [Parameter]
        public string? Class { get; set; }

        /// <summary>
        /// Sets the CSS style for the outermost container div.
        /// </summary>
        [Parameter]
        public string? Style { get; set; }

        /// <summary>
        /// The orientation for the splitter.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="SplitterDirection.Horizontal"/>.
        /// </remarks>
        [Parameter]
        public SplitterDirection Direction { get; set; } = SplitterDirection.Horizontal;

        /// <summary>
        /// Sets the height value for the splitter container.
        /// </summary>
        /// <remarks>Defaults to 100%</remarks>
        [Parameter]
        public string Height { get; set; } = "100%";

        /// <summary>
        /// Sets the width value for the splitter container. Can be any valid CSS unit.
        /// </summary>
        /// <remarks>Defaults to 100%</remarks>
        [Parameter]
        public string Width { get; set; } = "100%";

        /// <summary>
        /// The template for the separator, always in horizontal orientation. When used in vertical mode, the separator is rotated 90 degrees.
        /// </summary>
        [Parameter]
        public RenderFragment? SeparatorTemplate { get; set; }

        /// <summary>
        /// Sets the content in the left panel region when in horizontal mode, or the top panel region when in vertical mode.
        /// </summary>
        [Parameter, EditorRequired]
        public RenderFragment StartSplitter { get; set; }

        /// <summary>
        /// Sets the content in the right panel region when in horizontal mode, or the bottom panel region when in vertical mode.
        /// </summary>
        [Parameter, EditorRequired]
        public RenderFragment EndSplitter { get; set; }

        /// <summary>
        /// Gets a value indicating whether dragging is currently allowed.
        /// </summary>
        private bool CanDrag => _dragging && _points is not null && _viewportSize is not null;

        /// <summary>
        /// override OnAfterRenderAsync to import the JS module for drag handling.
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                _module = await _jSRuntime.InvokeAsync<IJSObjectReference>("import", AssemblyInfo.ModulePath("mudxSplitter.js"));
            }
        }

        private async Task OnPointerDown(PointerEventArgs args)
        {
            // Handle pointer down event
            await SetDraggingState(true, args);
            // set starting point after extending pointer capture and starting drag
            _points = new DragPoints(args.ClientX, args.ClientY, _startSize);
        }

        private async Task OnPointerUp(PointerEventArgs args)
        {
            // Handle pointer up event
            // perform a final "move" checking original points
            if (!CanDrag)
            {
                // If not dragging or points are not set, we do nothing
                return;
            }

            PerformPointerDrag(_points!.Value.XDown, _points!.Value.YDown,
                args.ClientX, args.ClientY, _points!.Value.StartSize);

            await SetDraggingState(false, args);
        }

        private void OnPointerMove(PointerEventArgs args)
        {
            // Handle pointer move event
            // If the sheet is open and dragging and points set, we handle the pointer move
            if (!CanDrag) return;

            // Throttle pointer move events to avoid excessive updates
            var now = DateTime.UtcNow;
            if (now - _lastPointerMove < PointerMoveThrottle)
                return;

            _lastPointerMove = now;

            PerformPointerDrag(_points!.Value.XDown, _points!.Value.YDown,
                args.ClientX, args.ClientY, _points!.Value.StartSize);
        }

        private Task OnPointerCancel(PointerEventArgs args)
        {
            // Handle pointer cancel event
            return SetDraggingState(false, args);
        }


        /// <summary>
        /// Sets the dragging state of the sheet.
        /// </summary>
        /// <param name="isDragging">A boolean indicating whether the sheet is in a dragging operation.</param>
        /// <param name="args">The pointer event arguments containing details about the pointer interaction.</param>
        private async Task SetDraggingState(bool isDragging, PointerEventArgs args)
        {
            _dragging = isDragging;
            // Reset the swipe deltas when starting or stopping dragging
            _points = null;
            _viewportSize = null;

            if (_module == null)
                return;

            if (isDragging)
            {
                var sizeArray = await _module.InvokeAsync<double[]>("startDrag", _dragElement, args.PointerId);
                if (sizeArray is not { Length: 2 })
                {
                    throw new InvalidOperationException("JSInterop did not return the expected MudSheet size array.");
                }
                _viewportSize = new ViewPortSize(sizeArray[0], sizeArray[1]);
            }
            else
            {
                await _module.InvokeVoidAsync("cancelDrag", _dragElement, args.PointerId);
            }
        }

        /// <summary>
        /// Set's the new size during a drag operation based on the pointer movement.
        /// </summary>
        /// <param name="baseSize">Size of the sheet at the starting point.</param>
        /// <param name="startX">x coordinate of the starting point.</param>
        /// <param name="startY">y coordinate of the starting point.</param>
        /// <param name="currentX">x coordinate of the current point.</param>
        /// <param name="currentY">y coordinate of the current point.</param>
        private void PerformPointerDrag(double startX, double startY, double currentX, double currentY, int baseSize)
        {
            // Get pixel movement in the appropriate direction
            var delta = Direction switch
            {
                SplitterDirection.Horizontal => currentX - startX,
                SplitterDirection.Vertical => currentY - startY,
                _ => throw new NotImplementedException()
            };

            var viewportPixels = Direction switch
            {
                SplitterDirection.Horizontal => _viewportSize!.Value.Width,
                SplitterDirection.Vertical => _viewportSize!.Value.Height,
                _ => throw new NotImplementedException()
            };

            // Convert pixel movement into percentage of viewport
            var newSize = baseSize + (delta / viewportPixels * 100);

            _startSize = Math.Clamp((int)newSize, 0, 100);
            StateHasChanged();
        }

        /// <summary>
        /// overridew DisposeAsync to clean up the JS module reference
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposing)
                return;

            _disposing = true;

            if (_module is not null)
            {
                await _module.DisposeAsync();
                _module = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
