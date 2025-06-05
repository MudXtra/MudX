using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class CopyToClipboardBasicExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "CopyToClipboardBasicExample.razor",
                Code: @"@namespace MudX.Docs.CopyToClipboard

<style>
    .set-text {
        width: 300px;
    }

        .set-text input {
            text-align: center;
        }
</style>
<MudStack Class=""d-flex mx-auto"">
    <MudTextField Class=""set-text"" @bind-Value=""_text"" Label=""Text To Copy"" Adornment=""Adornment.End"" AdornmentIcon=""@Icons.Material.Outlined.ContentCopy"" OnAdornmentClick=""@CopyClick"" />
    <MudSwitch Label=""Snackbar"" @bind-Value=""_snackbar"" />
</MudStack>

<MudXCopyToClipboard @ref=""_copyToClipboard"" Snackbar=""_snackbar"" />

@code {
    private MudXCopyToClipboard? _copyToClipboard;
    private string _text = ""Text To Copy"";
    private bool _snackbar = true;

    private async Task CopyClick(MouseEventArgs args)
    {
        if (_copyToClipboard == null)
            return;

        var copyResult = await _copyToClipboard.CopyToClipboardAsync(_text);
    }
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
