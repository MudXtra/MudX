using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Utilities;
using MudX.Extensions;

namespace MudX
{
    /// <summary>
    /// A splitter component for allowing users to resize two adjacent panels. Can be oriented horizontally or vertically and nested within each other.
    /// </summary>
    public partial class MudXSplitter : ComponentBase
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

        /// <summary>
        /// The primary class used on the span surrounding the separator and/or separator template.
        /// </summary>
        protected string SeparatorClass =>
            new CssBuilder("mudx-splitter-slider")
            .AddClass($"mudx-splitter-{Direction.ToDescription()}")
            .Build();

        [Inject]
        private IJSRuntime _jSRuntime { get; set; } = default!;

        /// <summary>
        /// The orientation for the splitter.
        /// </summary>
        [Parameter]
        public SplitterDirection Direction { get; set; } = SplitterDirection.Horizontal;

        /// <summary>
        /// The template for the separator, always in horizontal orientation. When used in vertical mode, the separator is rotated 90 degrees.
        /// </summary>
        [Parameter]
        public RenderFragment? SeparatorTemplate { get; set; }

        /// <summary>
        /// Gets a value indicating whether dragging is currently allowed.
        /// </summary>
        private bool CanDrag => _dragging && _points is not null && _viewportSize is not null;

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
            if (isDragging)
            {
                var sizeArray = await _jSRuntime.InvokeAsync<double[]>("window.xtraDrag.startDrag", _dragElement, args.PointerId);
                if (sizeArray is not { Length: 2 })
                {
                    throw new InvalidOperationException("JSInterop did not return the expected MudSheet size array.");
                }
                _viewportSize = new ViewPortSize(sizeArray[0], sizeArray[1]);
            }
            else
            {
                await _jSRuntime.InvokeVoidAsync("window.xtraDrag.cancelDrag", _dragElement, args.PointerId);
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
            var delta = currentY - startY;

            var viewportPixels = _viewportSize!.Value.Height;

            // Convert pixel movement into percentage of viewport
            var newSize = baseSize + (delta / viewportPixels * 100);

            _startSize = Math.Clamp((int)newSize, 0, 100);
            StateHasChanged();
        }
    }
}
