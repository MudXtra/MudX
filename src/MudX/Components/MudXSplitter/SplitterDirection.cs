using System.ComponentModel;

namespace MudX
{
    /// <summary>
    /// The orientation in which the splitter is oriented, either horizontal or vertical.
    /// </summary>
    /// <remarks>
    /// Horizontal means the <see cref="MudXSplitter.StartSplitter"/> splitter is the left side and <see cref="MudXSplitter.EndSplitter"/>  is the right side panel, 
    /// while Vertical means the <see cref="MudXSplitter.StartSplitter"/> splitter is the top side and <see cref="MudXSplitter.EndSplitter"/> is the bottom panel.
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
