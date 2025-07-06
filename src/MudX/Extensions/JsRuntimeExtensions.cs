using Microsoft.JSInterop;

namespace MudX
{
    /// <summary>
    /// Provides extension methods for interacting with JavaScript runtime functionality. Shortcuts for common 
    /// use javascript in the primary library
    /// </summary>
    /// <remarks>This class contains helper methods that extend the <see cref="IJSRuntime"/> interface, 
    /// enabling simplified interaction with JavaScript APIs.</remarks>
    public static class JsRuntimeExtensions
    {
        /// <summary>
        /// Copies the specified text to the clipboard using mudxGeneral.copyToClipboard method
        /// </summary>
        /// <param name="jsRuntime">as an Extension this is IJsRuntime</param>
        /// <param name="text">The text to copy</param>
        /// <returns>A string representing status, <c>success</c> is returned when the copy is completed successfully.</returns>
        public static async Task<string> CopyToClipboard(this IJSRuntime jsRuntime, string text)
        {
            var status = "IJSRuntime not initialized.";
            if (jsRuntime != null)
            {
                try
                {
                    status = await jsRuntime.InvokeAsync<string>("mudxGeneral.copyToClipboard", text);
                }
                catch (JSException e)
                {
                    status = e.Message;
                }
            }
            return status;
        }
    }
}
