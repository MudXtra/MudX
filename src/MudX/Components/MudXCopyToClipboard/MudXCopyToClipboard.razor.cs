using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MudX
{
    public partial class MudXCopyToClipboard : ComponentBase
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            if (firstRender)
                IsJSRuntimeAvailable = true;
        }

        /// <summary>
        /// Copies the Text parameter to the clipboard
        /// </summary>
        /// <param name="text">Text to copy, if null or not provided it will use the Text parameter</param>
        public async Task<CopyResult> CopyToClipboardAsync(string? text)
        {
            var result = new CopyResult { Success = false, Message = string.Empty };
            if (string.IsNullOrEmpty(text))
            {
                result.Message = "No text to copy";
                return result;
            }
            if (IsJSRuntimeAvailable)
            {
                result.Message = await JSRuntime.InvokeAsync<string>("mudxGeneral.copyToClipboard", text);
                if (result.Message == "success")
                {
                    result.Success = true;
                    result.Message = "Copied to clipboard";
                }
                return result;
            }
            result.Message = "JSRuntime is not available";
            return result;
        }

        /// <summary>
        /// Indicates if the JSRuntime is available and the CopyToClipboardAsync method can be called
        /// </summary>
        public bool IsJSRuntimeAvailable = false;
    }
}
