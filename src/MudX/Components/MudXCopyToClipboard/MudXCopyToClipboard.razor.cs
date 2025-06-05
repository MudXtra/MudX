using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace MudX
{
    public partial class MudXCopyToClipboard : ComponentBase
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] ISnackbar SnackbarService { get; set; } = default!;

        /// <summary>
        /// Indicates if the snackbar should be shown as a result of a copy operation. A success will be Severity.Success
        /// and an error will be Severity.Error.
        /// </summary>
        /// <remarks>Default is false</remarks>
        [Parameter] public bool Snackbar { get; set; }

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
            }
            else
            {
                result.Message = "JSRuntime is not available";
            }
            if (Snackbar)
            {
                SnackbarService.Add(result.Message, result.Success ? Severity.Success : Severity.Error);
            }
            return result;
        }

        /// <summary>
        /// Indicates if the JSRuntime is available and the CopyToClipboardAsync method can be called
        /// </summary>
        public bool IsJSRuntimeAvailable = false;
    }
}
