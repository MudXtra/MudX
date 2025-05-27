using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;
using MudX.Extensions;

namespace MudX
{
    public partial class MudXCodeDisplay : MudComponentBase, IAsyncDisposable
    {
        private string _languageClass = string.Empty;
        private CodeFile? _codeFile;
        private IJSObjectReference? _module;

        protected string Classname =>
            new CssBuilder("mudx-code-display")
                .AddClass(Class, !string.IsNullOrWhiteSpace(Class))
                .Build();

        protected string Stylename =>
            new StyleBuilder()
                .AddStyle(Style, !string.IsNullOrWhiteSpace(Style))
                .Build();

        [Inject]
        public IJSRuntime _js { get; set; } = default!;

        /// <summary>
        /// An IEnumerable of CodeFiles. Each CodeFile has a Name and the code.
        /// </summary>
        /// <remarks>At least one CodeFile is required, otherwise an exception is thrown.</remarks>
        [Parameter, EditorRequired]
        public required IEnumerable<CodeFile> Codes { get; set; }

        /// <summary>
        /// The Code Theme for Syntax Highlighting
        /// </summary>
        /// <remarks>Defaults to Prism Default</remarks>
        [Parameter]
        public CodeTheme Theme { get; set; } = CodeTheme.Default;

        /// <summary>
        /// The current active CodeFile
        /// </summary>
        public CodeFile CodeFile { get; private set; } = default!;

        protected override void OnInitialized()
        {
            if (!Codes.Any())
                throw new Exception("MudXCodeDisplay must have at least one CodeFile");

            CodeFile = Codes.First();
            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/MudX/modules/mudxPrismWrapper.js");
            }
            // do we regenerate code every re-render?
            if (_codeFile != CodeFile && _module != null)
            {
                _codeFile = CodeFile;
                _languageClass = $"language-{_codeFile.Language.ToDescription()}";
                await _module.InvokeVoidAsync("highlightAll");
                StateHasChanged();
            }
        }

        private MarkupString PrismTheme =>
            new($"<link href=\"./_content/MudX/prism/prism-{Theme.ToDescription()}.css\" rel=\"stylesheet\" type=\"text/css\" />");

        public async ValueTask DisposeAsync()
        {
            if (_module != null)
            {
                await _module.DisposeAsync();
            }
        }
    }
}
