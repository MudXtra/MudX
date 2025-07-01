using System.ComponentModel;

namespace MudX
{
    /// <summary>
    /// Represents the available themes for code syntax highlighting.
    /// </summary>
    /// <remarks>Each theme corresponds to a predefined style for rendering code, typically used in syntax
    /// highlighting. Themes can be selected based on user preference or application requirements.</remarks>
    public enum CodeTheme
    {
        /// <summary>
        /// Represents the default value or state for the enumeration.
        /// </summary>
        [Description("default")]
        Default,
        /// <summary>
        /// Represents the "Okaidia" theme, used for syntax highlighting and styling purposes.
        /// </summary>
        /// <remarks>This theme is commonly associated with a dark color scheme and vibrant syntax
        /// highlighting. Use this value to apply the Okaidia theme in supported contexts.</remarks>
        [Description("okaidia")]
        Okaidia,
        /// <summary>
        /// Represents the dark theme, used for syntax highlighting and styling purposes.
        /// </summary>
        [Description("dark")]
        Dark,
        /// <summary>
        /// Represents the "funky" theme, used for syntax highlighting and styling purposes.
        /// </summary>
        [Description("funky")]
        Funky,
        /// <summary>
        /// Represents the "twilight" theme, used for syntax highlighting and stylign purposes.
        /// </summary>
        [Description("twilight")]
        Twilight,
        /// <summary>
        /// Represents the "coy" theme, used for syntax highlighting and styling purposes.
        /// </summary>
        [Description("coy")]
        Coy,
        /// <summary>
        /// Represents the Solarized Light theme, used for syntax highlighting and styling purposes.
        /// </summary>
        /// <remarks>The Solarized Light theme is a color scheme designed for readability and aesthetics, 
        /// featuring a light background with carefully chosen colors for text and code elements.</remarks>
        [Description("solarizedlight")]
        SolarizedLight,
        /// <summary>
        /// Represents the Tomorrow Night theme, used for syntax highlighting and styling purposes. 
        /// </summary>        
        [Description("tomorrownight")]
        TomorrowNight,
        /// <summary>
        /// Represents the Material Light theme, used for syntax highlighting and styling purposes.
        /// </summary>        
        [Description("materiallight")]
        MaterialLight,
        /// <summary>
        /// Represents the Material Dark theme, used for syntax highlighting and styling purposes.
        /// </summary>
        [Description("materialdark")]
        MaterialDark
    }
}
