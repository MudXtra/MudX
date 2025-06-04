using MudBlazor;

namespace MudX.Components.MudXSecurityCode
{
    public class CodeItem
    {
        public string Value { get; set; } = string.Empty;
        public char PatternChar { get; set; }
        public bool IsEditable { get; set; }
        public MudTextField<string>? TextFieldRef { get; set; }
    }
}
