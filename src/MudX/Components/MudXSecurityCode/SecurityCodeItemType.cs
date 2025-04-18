using System.ComponentModel;

namespace MudX.Components.MudXSecurityCode
{
    /// <summary>
    /// Represents the type of input allowed for a specific box in the security code.
    /// </summary>
    public enum SecurityCodeItemType
    {
        /// <summary>
        /// Only numeric values (0-9) are allowed.
        /// </summary>
        [Description("number")]
        Numeric = 0,

        /// <summary>
        /// Only alphabetic characters (A-Z, a-z) are allowed.
        /// </summary>
        [Description("alpha")]
        Alpha = 1,

        /// <summary>
        /// Both alphabetic characters and numeric values are allowed.
        /// </summary>
        [Description("alphaNumeric")]
        AlphaNumeric = 2,

        /// <summary>
        /// Special symbols (e.g., @, #, $, etc.) are allowed.
        /// </summary>
        [Description("symbol")]
        Symbol = 3,

        /// <summary>
        /// Used to represent a delimiter (e.g., a dash or space) between code segments.
        /// </summary>
        [Description("delimiter")]
        Delimiter = 4,

        /// <summary>
        /// All types of input are allowed (numeric, alphabetic, symbols, etc.).
        /// </summary>
        [Description("all")]
        All = 99
    }
}
