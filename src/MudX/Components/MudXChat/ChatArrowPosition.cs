using System.ComponentModel;

namespace MudX;

/// <summary>
/// The position of the Chat Bubble Arrow
/// </summary>
public enum ChatArrowPosition
{
    /// <summary>
    /// The arrow is attached to the top.
    /// </summary>
    [Description("top")]
    Top,

    /// <summary>
    /// The arrow is attached to the middle.
    /// </summary>
    [Description("middle")]
    Middle,

    /// <summary>
    /// The arrow is attached to the bottom.
    /// </summary>
    [Description("bottom")]
    Bottom,

    /// <summary>
    /// The arrow is not shown.
    /// </summary>
    [Description("none")]
    None,
}
