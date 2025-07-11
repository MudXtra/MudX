
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
        private IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// OnAfterRenderAsync override
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                bool isDev = true;
                string version = DateTime.Now.ToFileTimeUtc().ToString();
#if !DEBUG
    version = typeof(MudX._Imports).Assembly.GetName().Version?.ToString() ?? version;
    isDev = false;
#endif
                _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/MudX/modules/mudxProvider.js");
                var result = await _jsModule.InvokeAsync<bool>("initialize", version, isDev);
                if (!result)
                {
                    throw new InvalidOperationException("Failed to initialize MudX");
                }
            }
        }

        /// <summary>
        /// DisposeAsync
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            if (_jsModule != null)
            {
                await _jsModule.DisposeAsync();
                _jsModule = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
