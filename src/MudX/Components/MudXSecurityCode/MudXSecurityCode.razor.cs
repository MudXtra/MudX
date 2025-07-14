using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.State;
using MudBlazor.Utilities;

namespace MudX
{
    /// <summary>
    /// Represents a security code input component that allows users to enter a code based on a specified pattern.
    /// </summary>
    public partial class MudXSecurityCode : MudComponentBase, IAsyncDisposable
    {
        private ElementReference? _elementRef;
        private readonly string Id = $"mudx-code-id-{Guid.NewGuid()}";
        private DotNetObjectReference<MudXSecurityCode>? _dotNetRef;
        private readonly Dictionary<string, object?> _attributes = [];
        internal ParameterState<string?> _codeState;
        private bool _isInternalChange = false;
        private MudForm? _form = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="MudXSecurityCode"/> class.
        /// </summary>
        public MudXSecurityCode()
        {
            using var registerScope = CreateRegisterScope();
            _codeState = registerScope.RegisterParameter<string?>(nameof(Code))
                .WithParameter(() => Code)
                .WithEventCallback(() => CodeChanged)
                .WithChangeHandler(OnChangeHandler);
        }

        [Inject]
        private IJSRuntime Js { get; set; } = default!;

        private IJSObjectReference? _module;

        /// <summary>
        /// The List of CodeItems
        /// </summary>
        internal List<CodeItem> CodeItems = [];

        /// <summary>
        /// Gets the CSS class name for a code item based on its configuration.
        /// </summary>
        protected string CodeClassname => new CssBuilder("mudx-code-item")
            .AddClass("dense", Margin == Margin.Dense)
            .AddClass("vertical", !Horizontal)
            .Build();

        /// <summary>
        /// The width of the container which is CodeItems at 32 or 42px each plus spacing between items
        /// </summary>
        private string ContainerClass =>
            $"<style> " +
            $"#{Id} {{ width: {CodeItems.Count * (Margin == Margin.Dense ? 32 : 42) + (CodeItems.Count - 1) * Spacing}px; }} " +
            $"</style>";

        /// <summary>
        /// The pattern of the security code. e.g. ("<c>##/##/19##</c>" would represent "MM/DD/YYYY" where the first two YY are filled in.).
        /// <para><b>Default Placeholders (can be changed)</b>>.</para>
        /// <para># is a placeholder for a numeric value.</para>
        /// <para>A is a placeholder for an alphabetic character.</para>
        /// <para>? is a placeholder for an alphabetic or numeric character.</para>
        /// <para>@ is a placeholder for a special symbol.</para>
        /// <para>* is a placeholder for any UTF-8 character.</para>
        /// </summary>
        /// <remarks>Defaults to "####"</remarks>
        [Parameter]
        public string Pattern { get; set; } = "####";

        /// <summary>
        /// The placeholder character for numeric values.
        /// </summary>
        /// <remarks>Defaults to '#', translates to char.IsDigit </remarks>
        [Parameter]
        public char PlaceholderNumeric { get; set; } = '#';

        /// <summary>
        /// The placeholder character for alphabetic characters.
        /// </summary>
        /// <remarks>Defaults to 'A', translates to char.IsLetter </remarks>
        [Parameter]
        public char PlaceholderAlpha { get; set; } = 'A';

        /// <summary>
        /// The placeholder character for alphabetic or numeric characters.
        /// </summary>
        /// <remarks>Defaults to '?', translates to char.IsLetterOrDigit </remarks>
        [Parameter]
        public char PlaceholderAlphaNumeric { get; set; } = '?';

        /// <summary>
        /// The placeholder character for special characters, typically symbols.
        /// </summary>
        /// <remarks>Defaults to '@', translates to char.IsSymbol or char.IsPunctuation </remarks>
        [Parameter]
        public char PlaceholderSpecial { get; set; } = '@';

        /// <summary>
        /// The placeholder character for any UTF-8 character.
        /// </summary>
        /// <remarks>Defaults to '*', translates to any UTF-8 char.</remarks>
        [Parameter]
        public char PlaceholderAny { get; set; } = '*';

        /// <summary>
        /// The current value of the security code.
        /// </summary>
        /// <remarks>Defaults to <c>null</c>.</remarks>
        [Parameter]
        public string? Code { get; set; }

        /// <summary>
        /// Called when the value of the security code changes.
        /// </summary>
        [Parameter]
        public EventCallback<string?> CodeChanged { get; set; }

        /// <summary>
        /// If true, each input will be masked as a password.
        /// </summary>
        /// <remarks>Defaults to <c>false</c>.</remarks>
        [Parameter]
        public bool Password { get; set; } = false;

        /// <summary>
        /// The gap between items in increments of 4px. 
        /// </summary>
        /// <remarks>Defaults to 3, maximum of 20 (80px)</remarks>
        [Parameter]
        public int Spacing { get; set; } = 2;

        /// <summary>
        /// Whether to display items horizontally, if true will display items horizontally, false will display items vertically
        /// </summary>
        /// <remarks>Defaults to <c>true</c>.</remarks>
        [Parameter]
        public bool Horizontal { get; set; } = true;

        /// <summary>
        /// <para>The display variant of the component. Options are Outlined, Text, and Filled.</para>
        /// </summary>
        /// <remarks>Defaults to <see cref="Variant.Outlined" /></remarks>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Outlined;

        /// <summary>
        /// <para>The display variant of the code when readonly. Options are Outlined, Text, and Filled.</para>
        /// </summary>
        /// <remarks>Defaults to <see cref="Variant.Text" /></remarks>
        [Parameter]
        public Variant ReadOnlyVariant { get; set; } = Variant.Text;

        /// <summary>
        /// If true, the underline will be visible.
        /// </summary>
        /// <remarks>Defaults to <c>true</c>.</remarks>
        [Parameter]
        public bool Underline { get; set; } = true;

        /// <summary>
        /// The margin of the component.
        /// </summary>
        /// <remarks>Defaults to <see cref="Margin.Normal" /></remarks>
        [Parameter]
        public Margin Margin { get; set; } = Margin.Normal;

        /// <summary>
        /// Whether all the code boxes have valid inputs.
        /// </summary>
        public bool IsValid => _form?.IsValid ?? false;

        //public string FullCode => 

        /// <summary>
        /// OnParameterSet override
        /// </summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (UserAttributes is { Count: > 0 })
            {
                foreach (KeyValuePair<string, object?> attr in UserAttributes)
                    _attributes.TryAdd(attr.Key, attr.Value);
            }

            StateHasChanged();
        }

        /// <summary>
        /// OnInitialized override
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();
            _attributes.Add("autocomplete", "off");
            foreach (KeyValuePair<string, object?> attr in UserAttributes)
                _attributes.Add(attr.Key, attr.Value);
            GenerateFromPattern(Pattern);
        }

        /// <summary>
        /// OnAfterRenderAsync override
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                _module = await Js.InvokeAsync<IJSObjectReference>("import", "./_content/MudX/modules/mudxSecurityCode.js");
                await _module.InvokeVoidAsync("init", _dotNetRef, _elementRef);
            }
        }

        private IEnumerable<string> CharPatternValidator(int index, string val)
        {
            if (string.IsNullOrEmpty(val))
                yield return "*";
            else if (val.Length > 1 && !IsValidInput(CodeItems[index].PatternChar, val))
                yield return "*";
        }

        private void GenerateFromPattern(string pattern)
        {
            var index = 0;
            char[] patternList = [PlaceholderAlpha, PlaceholderNumeric, PlaceholderAlphaNumeric, PlaceholderSpecial, PlaceholderAny];
            CodeItems.Clear();
            foreach (var ch in pattern)
            {
                var isEditable = patternList.Contains(ch);
                CodeItems.Add(new CodeItem
                {
                    Index = index++,
                    MasterId = Id[13..], // Extract the unique part of the ID
                    Value = !isEditable ? ch.ToString() : string.Empty,
                    PatternChar = ch,
                    IsEditable = isEditable
                });
            }
        }

        internal async Task OnAfterChange(int index)
        {
            var input = CodeItems[index].Value;
            if (string.IsNullOrEmpty(input))
            {
                CodeItems[index].Value = string.Empty;
                await UpdateCodeValue();
                await MoveFocus(index);
                return;
            }

            var val = input[0].ToString(); // first char
            if (IsValidInput(CodeItems[index].PatternChar, val))
            {
                CodeItems[index].Value = val;
                // Find next editable index
                int next = index + 1;
                while (next < CodeItems.Count && !CodeItems[next].IsEditable)
                {
                    next++;
                }

                if (next < CodeItems.Count)
                {
                    await MoveFocus(next);
                }
                // We're at the last editable index — move to the next focusable element
                else if (index == CodeItems.Count - 1 && _module != null && (_form?.IsValid ?? false))
                    await _module.InvokeVoidAsync("focusNextAfterContainer", _elementRef);
            }
            else
            {
                CodeItems[index].Value = string.Empty;
            }
            var textFieldRef = CodeItems[index].TextFieldRef;
            if (textFieldRef != null && index < CodeItems.Count - 1)
                await textFieldRef.Validate();
            else
            {
                _form?.Validate().CatchAndLog();
            }
            await UpdateCodeValue();
        }

        private async Task OnKeyDown(int index, KeyboardEventArgs e)
        {
            if (e.Key == "Backspace" && string.IsNullOrEmpty(CodeItems[index].Value) && index > 0)
            {
                int prev = index - 1;
                while (prev >= 0 && !CodeItems[prev].IsEditable)
                {
                    prev--;
                }

                if (prev >= 0)
                {
                    await MoveFocus(prev);
                }
            }
        }

        private async Task MoveFocus(int index)
        {
            if (index >= 0 && index < CodeItems.Count && CodeItems[index].IsEditable && _module != null)
            {
                await _module.InvokeVoidAsync("focusBlock", _elementRef, CodeItems[index].InputId);
            }
        }

        private bool IsValidInput(char pattern, string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            char ch = value[0];

            if (pattern == PlaceholderNumeric)
                return char.IsDigit(ch);

            if (pattern == PlaceholderAlpha)
                return char.IsLetter(ch);

            if (pattern == PlaceholderAlphaNumeric)
                return char.IsLetterOrDigit(ch);

            if (pattern == PlaceholderSpecial)
                return char.IsSymbol(ch) || char.IsPunctuation(ch);

            if (pattern == PlaceholderAny)
                return true;

            return false;
        }

        /// <summary>
        /// Handles the clipboard paste event triggered in javascript by processing the pasted text and updating the corresponding code items.
        /// </summary>
        /// <remarks>This method processes the pasted text by matching it against the editable and fixed
        /// characters in the code items. Editable code items are updated with valid characters from the pasted text,
        /// while fixed code items are set to their predefined values. After processing, the method updates the code
        /// value, moves focus to the next focusable item, and validates the form if applicable.</remarks>
        /// <param name="fullid">The full identifier string, which must be at least 10 characters long. The substring after the first 10
        /// characters is used to determine the starting index for processing.</param>
        /// <param name="text">The text pasted from the clipboard. Cannot be null, empty, or consist only of whitespace.</param>
        /// <returns></returns>
        [JSInvokable]
        public async Task ClipboardPasteEvent(string fullid, string text)
        {
            if (string.IsNullOrWhiteSpace(text) || fullid.Length <= 10)
                return;

            // Extract the substring starting at index 10 and ending before the next dash.
            var id = fullid[10..];
            var parts = id.Split("-");
            id = parts[0];

            if (!int.TryParse(id, out int index))
                return;

            var chars = text.ToCharArray();
            int charIndex = 0;

            for (int i = index; i < CodeItems.Count && charIndex < chars.Length; i++)
            {
                var patternChar = CodeItems[i].PatternChar;

                var isEditable = CodeItems[i].IsEditable;
                var pasteChar = chars[charIndex].ToString();

                if (isEditable)
                {
                    // Skip any invalid characters until we find a match
                    while (charIndex < chars.Length &&
                           !IsValidInput(patternChar, chars[charIndex].ToString()))
                    {
                        charIndex++;
                    }

                    if (charIndex >= chars.Length)
                        break;

                    pasteChar = chars[charIndex].ToString();
                    CodeItems[i].Value = pasteChar;
                    charIndex++;
                }
                else
                {
                    // Fixed character like '/', or '1', '9'
                    if (charIndex >= chars.Length)
                        break;

                    if (chars[charIndex] == patternChar)
                    {
                        // Match – consume and move forward
                        CodeItems[i].Value = patternChar.ToString();
                        charIndex++;
                    }
                    else
                    {
                        // Mismatch – don't consume, but still set the fixed value
                        // This allows skipping over fixed parts even if missing in input
                        CodeItems[i].Value = patternChar.ToString();
                        // DO NOT increment charIndex here
                    }
                }
            }

            await UpdateCodeValue();

            // Move to next focusable item
            int nextIndex = CodeItems.FindLastIndex(ci => !string.IsNullOrEmpty(ci.Value)) + 1;
            if (nextIndex < CodeItems.Count)
            {
                await MoveFocus(nextIndex);
            }
            else if (_module != null)
            {
                await _module.InvokeVoidAsync("focusNextAfterContainer", _elementRef);
            }

            if (_form != null)
                await _form.Validate();
        }

        private async Task UpdateCodeValue()
        {
            var result = string.Empty;

            // Find the index of the last filled editable item
            int lastFilledEditableIndex = CodeItems
                .Select((item, index) => new { item, index })
                .Where(x => x.item.IsEditable && !string.IsNullOrEmpty(x.item.Value))
                .Select(x => x.index)
                .DefaultIfEmpty(-1)
                .Max();

            for (int i = 0; i < CodeItems.Count; i++)
            {
                var item = CodeItems[i];

                if (item.IsEditable)
                {
                    result += item.Value;
                }
                // Show fixed characters only if they appear before or at the last filled editable,
                // or if they are the first fixed character immediately after the last filled editable
                else if (i <= lastFilledEditableIndex ||
                         (i > lastFilledEditableIndex &&
                          lastFilledEditableIndex >= 0 &&
                          CodeItems[i - 1].IsEditable &&
                          i == lastFilledEditableIndex + 1))
                {
                    result += item.PatternChar;
                }
            }

            _isInternalChange = true;
            await _codeState.SetValueAsync(result);
            await CodeChanged.InvokeAsync(_codeState.Value);
            _isInternalChange = false;
            StateHasChanged();
        }

        private void OnChangeHandler(ParameterChangedEventArgs<string?> args)
        {
            // No change at all? Skip.
            if (args.Value == args.LastValue)
                return;

            // We triggered this change ourselves? Skip.
            if (_isInternalChange)
                return;

            // either initial bind-code value or the user updates the value externally
            if (string.IsNullOrEmpty(args.Value))
            {
                foreach (var item in CodeItems)
                    item.Value = string.Empty;
                StateHasChanged();
            }
        }

        /// <summary>
        /// DisposeAsync
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (IsJSRuntimeAvailable)
            {
                if (_module != null)
                {
                    await _module.InvokeVoidAsync("cleanup", _elementRef);
                    await _module.DisposeAsync();
                    _module = null;
                }
                _dotNetRef?.Dispose();
                _dotNetRef = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
