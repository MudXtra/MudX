using System.ComponentModel.DataAnnotations;
using MudBlazor;

namespace MudX
{
    /// <summary>
    /// Represents an item in a code input field, containing metadata such as its index, value, and editability.
    /// </summary>
    public class CodeItem
    {
        /// <summary>
        /// The index for the code item
        /// </summary>
        [Key]
        public int Index { get; set; }
        /// <summary>
        /// The current Value of the code item
        /// </summary>
        public string Value { get; set; } = string.Empty;
        /// <summary>
        /// The pattern character used for this code item
        /// </summary>
        public char PatternChar { get; set; }
        /// <summary>
        /// Whether this code item is read only or editable
        /// </summary>
        public bool IsEditable { get; set; }
        /// <summary>
        /// Gets the unique identifier for the input element.
        /// </summary>
        public string InputId => $"mudX-code-{Index}";
        /// <summary>
        /// The <c>@ref</c> value for the underlying MudTextField
        /// </summary>
        public MudTextField<string>? TextFieldRef = null!;
    }
}
