using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class IdMaskerBasicExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "IdMaskerBasicExample.razor",
                Code: @"@namespace MudX.Docs.IdMasker
@using Microsoft.JSInterop

<style>
    .set-text {
    width: 300px;
    }

    .set-text input {
    text-align: center;
    }
</style>

<MudStack>
    <MudTextField Class=""mx-auto set-text"" @bind-Value=""_text"" Label=""Id To Mask"" Adornment=""Adornment.End"" AdornmentIcon=""@Icons.Material.Outlined.Masks"" OnAdornmentClick=""@MaskClick"" />
    <MudText>
        Obfuscated: @_obfuscated
        @if (_obfuscated.Length > 0)
        {
            <MudIconButton Icon=""@Icons.Material.Filled.CopyAll"" OnClick=""@(() => _jsRuntime.CopyToClipboard(_obfuscated))"" Color=""Color.Primary"" />
        }        
    </MudText><br />
    <MudTextField Class=""mx-auto set-text"" @bind-Value=""_text2"" Label=""Mask to Id"" Adornment=""Adornment.End"" AdornmentIcon=""@Icons.Material.Outlined.Masks"" OnAdornmentClick=""@UnMaskClick"" />
    <MudText>
        Decoded: @_unobfuscated
    </MudText>
</MudStack>


@code {
    private string _text = ""1101203567"";
    private string _text2 = string.Empty;
    private string _obfuscated = string.Empty;
    private string _unobfuscated = string.Empty;

    [Inject]
    private IJSRuntime _jsRuntime { get; set; } = default!;

    private void MaskClick(MouseEventArgs args)
    {
        _obfuscated = MudX.IdMasker.MaskId(_text);
    }

    private void UnMaskClick(MouseEventArgs args)
    {
        _unobfuscated = MudX.IdMasker.UnMaskId(_text2);
    }
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
