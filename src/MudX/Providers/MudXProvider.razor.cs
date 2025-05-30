
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MudX
{
    public partial class MudXProvider
    {
        private IJSObjectReference? _jsModule;

        [Inject]
        private IJSRuntime _js { get; set; } = default!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _jsModule = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/MudX/modules/mudxProvider.js");
                var result = await _jsModule.InvokeAsync<bool>("initialize");
                if (!result)
                {
                    throw new InvalidOperationException("Failed to initialize MudX");
                }
            }
        }
    }
}
