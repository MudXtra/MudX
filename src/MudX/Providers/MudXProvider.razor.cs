
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudX.Utilities;

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
    version = AssemblyInfo.Version;
    isDev = false;
#endif
                var packageId = AssemblyInfo.PackageId;
                _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", AssemblyInfo.ModulePath("mudxProvider.js"));
                var result = await _jsModule.InvokeAsync<bool>("initialize", version, isDev, packageId);
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
