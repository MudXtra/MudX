using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudX.Components.MudXSecurityCode;

namespace MudX
{
    public partial class MudXSecurityCode : MudComponentBase
    {

        protected List<CodeItem> CodeItems = [];

        /// <summary>
        /// The pattern of the security code. e.g. ("<c>##/##/19##</c>" would represent "MM/DD/YYYY" where the first two YY are filled in.).
        /// <para><b>Default Placeholders (can be changed)</b>>.</para>
        /// <para># is a placeholder for a numeric value.</para>
        /// <para>A is a placeholder for an alphabetic character.</para>
        /// <para>@ is a placeholder for a special symbol.</para>
        /// <para>* is a placeholder for any UTF-8 character.</para>
        /// </summary>
        /// <remarks>Defaults to "####"</remarks>
        [Parameter]
        public string Pattern { get; set; } = "####";

        [Parameter]
        public char PlaceholderNumeric { get; set; } = '#';

        [Parameter]
        public char PlaceholderAlpha { get; set; } = 'A';

        [Parameter]
        public char PlaceholderSpecial { get; set; } = '@';

        [Parameter]
        public char PlaceholderAny { get; set; } = '*';

        /// <summary>
        /// The current value of the security code.
        /// </summary>
        /// <remarks>Defaults to <langword="null" /></remarks>
        [Parameter]
        public string? Code { get; set; }

        /// <summary>
        /// Called when the value of the security code changes.
        /// </summary>
        [Parameter]
        public EventCallback<string> CodeChanged { get; set; }

        /// <summary>
        /// If true, each input will be masked as a password.
        /// </summary>
        /// <remarks>Defaults to <langword="false" /></remarks>
        [Parameter]
        public bool Password { get; set; } = false;

        /// <summary>
        /// The gap between items in increments of 4px. 
        /// </summary>
        /// <remarks>Defaults to 3, maximum of 20 (80px)</remarks>
        [Parameter]
        public int Spacing { get; set; } = 3;

        /// <summary>
        /// Displays items horizontally, if true will display items horizontally, false will display items vertically
        /// </summary>
        /// <remarks>Defaults to <langword="true" /></remarks>
        [Parameter]
        public bool Row { get; set; } = true;

        /// <summary>
        /// <para>The display variant of the component. Options are Outlined, Text, and Filled.</para>
        /// </summary>
        /// <remarks>Defaults to <see cref="Variant.Outlined" /></remarks>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Outlined;

        /// <summary>
        /// If true, the underline will be visible.
        /// </summary>
        /// <remarks>Defaults to <langword="true" /></remarks>
        [Parameter]
        public bool Underline { get; set; } = true;

        /// <summary>
        /// The margin of the component. Can not be overridden by nested <see cref="MudX.MudXCodeItem"/>
        /// </summary>
        /// <remarks>Defaults to <see cref="Margin.Normal" /></remarks>
        [Parameter]
        public Margin Margin { get; set; } = Margin.Normal;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            GenerateFromPattern(Pattern);
        }

        private void GenerateFromPattern(string pattern)
        {
            CodeItems.Clear();
            foreach (var ch in pattern)
            {
                CodeItems.Add(new CodeItem
                {
                    PatternChar = ch,
                    IsEditable = $"{PlaceholderAlpha}{PlaceholderNumeric}{PlaceholderSpecial}{PlaceholderAny}".Contains(ch)
                });
            }
        }

        private async Task OnInput(int index, ChangeEventArgs e)
        {
            var input = e.Value?.ToString();
            if (string.IsNullOrEmpty(input)) return;

            var val = input[0].ToString(); // first char
            if (IsValidInput(CodeItems[index].PatternChar, val))
            {
                CodeItems[index].Value = val;
                await MoveFocus(index + 1);
                await UpdateCodeValue();
            }
        }

        private async Task OnKeyDown(int index, KeyboardEventArgs e)
        {
            if (e.Key == "Backspace" && string.IsNullOrEmpty(CodeItems[index].Value))
            {
                await MoveFocus(index - 1);
            }
        }

        private async Task MoveFocus(int index)
        {
            if (index >= 0 && index < CodeItems.Count && CodeItems[index].IsEditable)
            {
                var task = CodeItems[index].TextFieldRef?.InputReference?.ElementReference.FocusAsync();
                if (task.HasValue)
                    await task.Value;
            }
        }

        private bool IsValidInput(char pattern, string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            char ch = value[0];

            if (pattern == PlaceholderNumeric)
                return char.IsDigit(ch);

            if (pattern == PlaceholderAlpha)
                return char.IsLetterOrDigit(ch);

            if (pattern == PlaceholderSpecial)
                return char.IsSymbol(ch) || char.IsPunctuation(ch);

            if (pattern == PlaceholderAny)
                return true;

            return false;
        }

        private async Task UpdateCodeValue()
        {
            var joined = string.Concat(CodeItems.Select(ci => ci.Value));
            Code = joined;
            await CodeChanged.InvokeAsync(joined);
        }
    }
}
