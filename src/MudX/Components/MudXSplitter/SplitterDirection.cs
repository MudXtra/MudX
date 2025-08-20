using System.ComponentModel;

namespace MudX
{
    /// <summary>
    /// The orientation in which the splitter is oriented, either horizontal or vertical.
    /// </summary>
    /// <remarks>
    /// Horizontal means the splitter has a <see cref="LeftSplitter"/> and <see cref="RightSplitter"/> panel, 
    /// while Vertical means the splitter has a <see cref="TopSplitter"/> and <see cref="BottomSplitter"/> panel.
    /// </remarks>
    public enum SplitterDirection
    {
        /// <summary>
        /// Specifies a horizontal alignment or orientation.
        /// </summary>
        [Description("horizontal")]
        Horizontal,

        /// <summary>
        /// Specifies a vertical alignment or orientation.
        /// </summary>
        [Description("vertical")]
        Vertical
    }
}
