using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace MudX
{
    /// <summary>
    /// Provides functionality to copy text to the clipboard and optionally display a snackbar notification.
    /// </summary>
    /// <remarks>This component uses JavaScript interop to perform clipboard operations. It does not generate
    /// anything in the DOM but provides a public method to copy something to clipboard. It ensures JSInterop
    /// is available before the copy and advises if not. </remarks>
    public partial class MudXCopyToClipboard : ComponentBase
    {
        [Inject] internal IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] internal ISnackbar SnackbarService { get; set; } = default!;

        /// <summary>
        /// Indicates if the snackbar should be shown as a result of a copy operation. A success will be Severity.Success
        /// and an error will be Severity.Error.
        /// </summary>
        /// <remarks>Default is false</remarks>
        [Parameter] public bool Snackbar { get; set; }

        /// <summary>
        /// OnAfterRender override
        /// </summary>
        /// <param name="firstRender"></param>
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
                result.Message = await JSRuntime.CopyToClipboard(text);
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
