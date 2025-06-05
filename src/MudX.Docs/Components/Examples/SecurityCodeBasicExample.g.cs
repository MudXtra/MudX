using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class SecurityCodeBasicExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "SecurityCodeBasicExample.razor",
                Code: @"@namespace MudX.Docs.SecurityCode

<style>
    .set-width {
        max-width: 300px;
        text-align: center;
    }
</style>

<MudTextField Class=""d-flex mx-auto set-width"" Immediate=""true"" FullWidth=""false"" Label=""Pattern"" @bind-Value=""_pattern"" />

<MudXSecurityCode @key=""_pattern"" Class=""d-flex mx-auto"" @bind-Code=""_code"" Pattern=""@_pattern"" />

<MudText Class=""d-flex mx-auto"" Color=""Color.Info"">
    Security Code: @_code
</MudText>

@code {
    private string? _code;
    private string _pattern = ""####"";
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
