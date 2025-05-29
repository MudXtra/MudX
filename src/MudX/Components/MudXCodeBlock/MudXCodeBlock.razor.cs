using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;
using MudX.Extensions;

namespace MudX
{
    /// <summary>
    /// Displays Code Files with Syntax Highlighting using Prism.js. Add-ons include Autolinker, Match Braces (toggleable, <c>true</c> by default),
    /// Line Numbers(toggleable, <c>false</c> by default) and Invisibles(toggleable, <c>false</c> by default).
    /// </summary>
    public partial class MudXCodeBlock : MudComponentBase, IAsyncDisposable
    {
        private bool _isRendered = false;
        private string _elementId = $"mudx-code-element-{Guid.NewGuid()}";
        private string _styleId = $"mudx-code-style-{Guid.NewGuid()}";
        private MudPopover? _popover;
        private bool _generateCode = true;
        private IJSObjectReference? _module;
        private bool _showCopy = false;
        private int _codeFileCount = 0;
        private int _index = 0;
        private System.Timers.Timer? _copyTimer;
        private CancellationTokenSource? _cts;
        private string _copyMessage = string.Empty;
        private bool _copySuccess = true;
        private bool _showMessage = false;

        private string PrismCSSPath =>
            new($"./_content/MudX/prism/prism-{Theme.ToDescription()}.css");

        private Placement GetPlacement(Origin origin) => origin switch
        {
            Origin.TopLeft or Origin.CenterLeft or Origin.BottomLeft => Placement.Right,
            Origin.TopRight or Origin.CenterRight or Origin.BottomRight => Placement.Left,
            Origin.BottomCenter => Placement.Top,
            _ => Placement.Bottom
        };

        private string GetInvisiblesStyle() => Invisibles
            ? string.Empty
            : $$"""
              #{{_elementId}} .token.space,
              #{{_elementId}} .token.crlf,
              #{{_elementId}} .token.tab,
              #{{_elementId}} .token.cr,
              #{{_elementId}} .token.lf {
                  opacity: 0 !important;
              }
             """;

        protected string Classname =>
            new CssBuilder("mudx-code-display")
                .AddClass(Class, !string.IsNullOrWhiteSpace(Class))
                .AddClass("invisible", !_isRendered)
                .Build();

        protected string Stylename =>
            new StyleBuilder()
                .AddStyle(Style, !string.IsNullOrWhiteSpace(Style))
                .Build();

        protected string CodeClass(string lang) => new CssBuilder()
            .AddClass($"lang-{lang}", !string.IsNullOrWhiteSpace(lang))
            .AddClass($"lang-html", string.IsNullOrWhiteSpace(lang))
            .AddClass("line-numbers", LineNumbers)
            .AddClass("match-braces", MatchBraces)
            .Build();

        protected string CopyButtonClass => new CssBuilder("mud-theme-transparent")
            .AddClass("mudx-code-tabs", _codeFileCount > 1)
            .Build();

        [Inject]
        public IJSRuntime _js { get; set; } = default!;

        /// <summary>
        /// An IEnumerable of CodeFiles. Each CodeFile has an Id, title, language, and code. You can provide as many CodeFiles as you want though both
        /// this parameter and as CodeBlock child components.
        /// </summary>
        /// <remarks>At least one CodeFile is required, otherwise an exception is thrown.</remarks>
        [Parameter, EditorRequired]
        public required IEnumerable<CodeFile> Codes { get; set; } = [];

        /// <summary>
        /// The Code Theme for Syntax Highlighting
        /// </summary>
        /// <remarks>Defaults to Prism Coy</remarks>
        [Parameter]
        public CodeTheme Theme { get; set; } = CodeTheme.MaterialLight;

        /// <summary>
        /// Enable Line Numbers for Syntax Highlighting
        /// </summary>
        /// <remarks>Defaults to false</remarks>
        [Parameter]
        public bool LineNumbers { get; set; }

        /// <summary>
        /// Show Invisibles for Syntax Highlighting such as CR, LF, Tab, and Space
        /// </summary>
        /// <remarks>Defaults to false</remarks>
        [Parameter]
        public bool Invisibles { get; set; }

        /// <summary>
        /// Show Match Braces for Syntax Highlighting so when you highlight one brace it highlights the corresponding brace.
        /// </summary>
        /// <remarks>Defaults to true</remarks>
        [Parameter]
        public bool MatchBraces { get; set; } = true;

        /// <summary>
        /// The Copy Button Origin, nullable field indicating where the copy button should be placed. If null no button will be rendered.
        /// </summary>
        /// <remarks>Defaults to <see cref="Origin.TopRight"/></remarks>
        [Parameter]
        public Origin? CopyOrigin { get; set; } = Origin.TopRight;

        /// <summary>
        /// The Copy Button Icon
        /// </summary>
        /// <remarks>Defaults to <see cref="Icons.Material.Filled.ContentCopy"/></remarks>
        [Parameter]
        public string CopyIcon { get; set; } = Icons.Material.Filled.ContentCopy;

        protected override void OnParametersSet()
        {
            if (_codeFileCount != Codes.Count())
            {
                _generateCode = true;
                _codeFileCount = Codes.Count();
                _isRendered = false;
                StateHasChanged();
            }
            base.OnParametersSet();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/MudX/modules/mudxPrismWrapper.js");
                await _module.InvokeAsync<bool>("initialize", PrismCSSPath, _styleId);
                StateHasChanged();
            }
            else if (_generateCode)
            {
                await GenerateCode();
                _isRendered = true;
                StateHasChanged();
            }
        }

        private async Task GenerateCode()
        {
            if (_module == null)
                return;
            await _module.InvokeVoidAsync("highlightElementById", _elementId);
            _generateCode = false;
        }

        private void ChangedTabIndex()
        {
            _isRendered = false;
            _generateCode = true;
            StateHasChanged();
        }

        private async Task CopyToClipboard(MouseEventArgs args)
        {
            // Clean up any previous timer/task
            _cts?.Cancel();
            _copyTimer?.Dispose();

            if (_module is null || Codes.Count() <= _index)
            {
                UpdateCopyStatus(false);
                return;
            }

            try
            {
                var currCode = Codes.ElementAt(_index);
                if (string.IsNullOrWhiteSpace(currCode?.Code))
                {
                    UpdateCopyStatus(false);
                    return;
                }

                // Use cancellation token with timeout
                _cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                _copySuccess = await _module.InvokeAsync<bool>(
                    "copyToClipboard",
                    _cts.Token,
                    currCode.Code.Trim()
                );

                UpdateCopyStatus(_copySuccess);
            }
            catch (OperationCanceledException)
            {
                UpdateCopyStatus(false, "Copy timed out");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Copy failed: {ex.Message}");
                UpdateCopyStatus(false, "Copy failed");
            }
        }

        private void UpdateCopyStatus(bool success, string? customMessage = null)
        {
            _copyMessage = customMessage ?? (success ? "Copied!" : "Failed to copy");
            _showMessage = true;

            // Dispose previous timer if exists
            _copyTimer?.Dispose();

            _copyTimer = new System.Timers.Timer(1500) { AutoReset = false };
            _copyTimer.Elapsed += (s, e) =>
            {
                InvokeAsync(() =>
                {
                    _showMessage = false;
                    StateHasChanged();
                });
            };
            _copyTimer.Start();
        }

        public async ValueTask DisposeAsync()
        {
            _cts?.Cancel();
            if (_copyTimer != null)
            {
                _copyTimer.Stop();
                _copyTimer.Dispose();
                _copyTimer = null;
            }
            _cts?.Dispose();
            _cts = null;
            if (_module != null)
            {
                await _module.DisposeAsync();
            }
            GC.SuppressFinalize(this);
        }

    }
}
