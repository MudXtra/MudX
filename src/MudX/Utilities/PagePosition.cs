using MudBlazor;
using MudBlazor.Interop;

namespace MudX.Utilities
{
    /// <summary>
    /// Provides utility methods for calculating page positions based on an element's bounding rectangle and anchor
    /// origin.
    /// </summary>
    /// <remarks>This class is designed to assist in determining the position of an element on a page relative
    /// to a specified anchor origin. The calculated position can be used for positioning overlays, popovers, or other
    /// UI elements.</remarks>
    public static class PagePosition
    {
        /// <summary>
        /// Converts the origin to a page position point of X and Y, MudPopover will translate that to a 1x1 box
        /// relative to that point.
        /// </summary>
        /// <param name="rect">BoundingClientRect typically gotten from ElementReference.MudGetBoundingClientRectAsync()</param>
        /// <param name="origin">The Anchor Origin</param>
        /// <returns></returns>
        public static (double X, double Y) GetPagePositionFromOrigin(BoundingClientRect rect, Origin origin)
        {
            var left = rect.AbsoluteLeft;
            var top = rect.AbsoluteTop;
            var right = rect.AbsoluteRight;
            var bottom = rect.AbsoluteBottom;
            var centerX = left + rect.Width / 2;
            var centerY = top + rect.Height / 2;

            return origin switch
            {
                Origin.TopLeft => (left, top),
                Origin.TopCenter => (centerX, top),
                Origin.TopRight => (right, top),

                Origin.CenterLeft => (left, centerY),
                Origin.CenterCenter => (centerX, centerY),
                Origin.CenterRight => (right, centerY),

                Origin.BottomLeft => (left, bottom),
                Origin.BottomCenter => (centerX, bottom),
                Origin.BottomRight => (right, bottom),

                _ => (centerX, centerY)
            };
        }
    }
}
