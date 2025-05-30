
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MudX
{
    /// <summary>
    /// Ensures the CSS and JS files are loaded for MudX
    /// </summary>
    /// <remarks>The MudXProvider should be placed in your MainLayout underneath the MudProviders.</remarks>
    public partial class MudXProvider : IAsyncDisposable
    {
        private IJSObjectReference? _jsModule;

        [Inject]
        private IJSRuntime _js { get; set; } = default!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                string version = DateTime.Now.ToFileTimeUtc().ToString();
#if !DEBUG
    version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? version;
#endif
                _jsModule = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/MudX/modules/mudxProvider.js");
                var result = await _jsModule.InvokeAsync<bool>("initialize", version);
                if (!result)
                {
                    throw new InvalidOperationException("Failed to initialize MudX");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_jsModule != null)
            {
                await _jsModule.DisposeAsync();
                _jsModule = null;
            }
        }
    }
}
