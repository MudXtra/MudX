using System.ComponentModel;

namespace MudX
{
    /// <summary>
    /// Specifies the available variants for Table of Contents styles in a user interface.
    /// </summary>
    public enum OutlineStyleVariant
    {
        /// <summary>
        /// Represents a bullet style.
        /// </summary>
        [Description("bullet")]
        Bullet = 0,
        /// <summary>
        /// Represents a scroll style.
        /// </summary>
        [Description("scroll")]
        Scroll = 1,
        /// <summary>
        /// Represents a minimal style showing a | character before each section
        /// </summary>
        [Description("minimal")]
        Minimal = 2,
        /// <summary>
        /// Represents no style apart from highlighting.
        /// </summary>
        [Description("none")]
        None = 99
    }
}
