using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;
using MudX.Extensions;
using MudX.Utilities;

namespace MudX;

/// <summary>
/// Displays Code Files with Syntax Highlighting using Prism.js. Add-ons include Autolinker, Match Braces (toggleable, <c>true</c> by default),
/// Line Numbers(toggleable, <c>false</c> by default) and Invisibles(toggleable, <c>false</c> by default).
/// </summary>
public partial class MudXCodeBlock : MudComponentBase, IAsyncDisposable
{
    private bool _isRendered = false;
    private readonly string _elementId = $"mudx-code-element-{Guid.NewGuid()}";
    private ElementReference _elementRef;
    private (double X, double Y) _position = (0, 0);
    private CodeTheme? _theme;
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

    internal string PrismCSSPath =>
        new($"./_content/{AssemblyInfo.PackageId}/prism/prism-{Theme.ToDescription()}.css");

    private static Placement GetPlacement(Origin origin) => origin switch
    {
        Origin.TopLeft or Origin.CenterLeft or Origin.BottomLeft => Placement.Right,
        Origin.TopRight or Origin.CenterRight or Origin.BottomRight => Placement.Left,
        Origin.BottomCenter => Placement.Top,
        _ => Placement.Bottom
    };

    private string GetInvisiblesStyle() => Invisibles
        ? string.Empty
        : $$"""
          <style class="invisible-hide">
          #{{_elementId}} .token.space,
          #{{_elementId}} .token.crlf,
          #{{_elementId}} .token.tab,
          #{{_elementId}} .token.cr,
          #{{_elementId}} .token.lf {
              opacity: 0 !important;
          }
          </style>
         """;

    /// <summary>
    /// Gets the CSS class name for the code display element, including any additional classes based on the current
    /// state.
    /// </summary>
    protected string Classname =>
        new CssBuilder("mudx-code-display")
            .AddClass(Class, !string.IsNullOrWhiteSpace(Class))
            .AddClass("invisible", !_isRendered)
            .Build();

    /// <summary>
    /// Gets the name of the style constructed using the current style settings.
    /// </summary>
    protected string Stylename =>
        new StyleBuilder()
            .AddStyle(Style, !string.IsNullOrWhiteSpace(Style))
            .Build();

    /// <summary>
    /// Builds a CSS class string based on the specified language and configuration settings.
    /// </summary>
    /// <param name="lang">The programming language identifier used to generate the CSS class. If null or whitespace, a default class
    /// for HTML is applied.</param>
    /// <returns>A string containing the constructed CSS class names based on the provided language and configuration.</returns>
    protected string CodeClass(string lang) => new CssBuilder()
        .AddClass($"lang-{lang}", !string.IsNullOrWhiteSpace(lang))
        .AddClass($"lang-html", string.IsNullOrWhiteSpace(lang))
        .AddClass("match-braces", MatchBraces)
        .Build();

    /// <summary>
    /// Builds a CSS class string for the current element, including the "line-numbers" class if applicable.
    /// </summary>
    /// <returns>A string containing the constructed CSS class names. The string will include "line-numbers" if the         
    /// <see cref="LineNumbers"/> condition is met.</returns>
    protected string PreClass() => new CssBuilder()
        .AddClass("line-numbers", LineNumbers)
        .Build();

    /// <summary>
    /// Gets the CSS class string for the copy button, dynamically built based on the current state.
    /// </summary>
    protected string CopyButtonClass => new CssBuilder("mud-theme-transparent")
        .AddClass("mudx-code-tabs", _codeFileCount > 1)
        .AddClass(CopyButtonClassname, !string.IsNullOrWhiteSpace(CopyButtonClassname))
        .Build();

    /// <summary>
    /// Gets the CSS class string for the copy button's popover, dynamically built based on its position.
    /// </summary>
    /// <remarks>The resulting CSS class string includes default styles for the copy button popover
    /// and position-specific overrides. This property is useful for ensuring consistent styling and alignment of
    /// the popover based on its origin.</remarks>
    protected string CopyPopoverClass => new CssBuilder("mudx-copy-button")
        .AddClass("mud-popover-position-override")
        .AddClass("mt-4", CopyOrigin?.ToDescription().StartsWith("top"))
        .AddClass("mt-n2", CopyOrigin?.ToDescription().StartsWith("center"))
        .AddClass("mt-n10", CopyOrigin?.ToDescription().StartsWith("bottom"))
        .AddClass("ml-4", CopyOrigin?.ToDescription().EndsWith("left"))
        .AddClass("ml-n10", CopyOrigin?.ToDescription().EndsWith("right"))
        .AddClass("ml-n5", CopyOrigin?.ToDescription().EndsWith("center"))
        .Build();

    /// <summary>
    /// Inline data attributes for positioning the copy icon at the coordinate location.
    /// </summary>
    private Dictionary<string, object> PositionAttributes => new()
    {
        { "data-pc-x", _position.X.ToString(CultureInfo.InvariantCulture) },
        { "data-pc-y", _position.Y.ToString(CultureInfo.InvariantCulture) }
    };

    /// <summary>
    /// Gets or sets the JavaScript runtime instance used for interop calls between .NET and JavaScript.
    /// </summary>
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    /// <summary>
    /// An IEnumerable of CodeFiles. Each CodeFile has an Id, title, language, and code. You can provide as many CodeFiles as you want though both
    /// this parameter and as CodeBlock child components.
    /// </summary>
    /// <remarks>At least one CodeFile is required, otherwise an exception is thrown.</remarks>
    [Parameter, EditorRequired]
    public required IEnumerable<CodeFile> Codes { get; set; } = [];

    /// <summary>
    /// The Prism Theme for Syntax Highlighting
    /// </summary>
    /// <remarks>Defaults to Prism Default</remarks>
    [Parameter]
    public CodeTheme Theme { get; set; } = CodeTheme.Default;

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

    /// <summary>
    /// The Copy Button Classname to add to the copy button
    /// </summary>
    /// <remarks>Defaults to null</remarks>
    [Parameter]
    public string CopyButtonClassname { get; set; } = string.Empty;

    /// <summary>
    /// OnParameterSetAsync overricde
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        if (_theme is null)
        {
            _theme = Theme;
        }
        else if (_theme != Theme && _module != null)
        {
            _theme = Theme;
            if (IsJSRuntimeAvailable)
                await _module.InvokeVoidAsync("injectCssFromFile", PrismCSSPath);
        }
        if (_codeFileCount != Codes.Count())
        {
            _codeFileCount = Codes.Count();
            _isRendered = false;
        }
        _generateCode = true;
        StateHasChanged();
        base.OnParametersSet();
    }

    /// <summary>
    /// OnAfterRenderAsync override
    /// </summary>
    /// <param name="firstRender"></param>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", AssemblyInfo.ModulePath("mudxPrismWrapper.js"));
            await _module.InvokeAsync<bool>("initialize", PrismCSSPath, AssemblyInfo.PackageId);
            StateHasChanged();
        }
        else if (_generateCode)
        {
            _isRendered = true;
            await GenerateCode();
        }
        // fallback to ensure copy button is in right spot if the component receives any sort of focus
        await PositionCopyButton();
    }

    /// <summary>
    /// Causes an immediate repositioning of the Copy Button
    /// </summary>
    public virtual async Task PositionCopyButton()
    {
        if (CopyOrigin.HasValue)
        {
            var boundingRect = await _elementRef.MudGetBoundingClientRectAsync();
            if (boundingRect != null)
                _position = PagePosition.GetPagePositionFromOrigin(boundingRect, CopyOrigin.Value);
        }
    }

    private async Task GenerateCode()
    {
        if (_module == null)
            return;
        await _module.InvokeVoidAsync("highlightElementById", _elementId);
        _generateCode = false;

        // Let Blazor flush DOM updates
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        await PositionCopyButton();
        StateHasChanged();
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
            var result = await JsRuntime.InvokeAsync<string>(
                "mudxGeneral.copyToClipboard",
                _cts.Token,
                currCode.Code.Trim()
            );

            _copySuccess = result == "success";

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
        _copySuccess = success;
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

    /// <summary>
    /// DisposeAsync
    /// </summary>
    /// <returns></returns>
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
