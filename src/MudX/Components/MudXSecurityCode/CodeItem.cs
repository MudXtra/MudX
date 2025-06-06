using System.ComponentModel.DataAnnotations;
using MudBlazor;

namespace MudX
{
    public class CodeItem
    {
        [Key]
        public int Index { get; set; }
        public string Value { get; set; } = string.Empty;
        public char PatternChar { get; set; }
        public bool IsEditable { get; set; }
        public string InputId => $"mudX-code-{Index}";
        public MudTextField<string>? TextFieldRef = null!;
    }
}
