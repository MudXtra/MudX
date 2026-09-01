using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class SecurityCodeFormStateExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "SecurityCodeFormStateExample.razor",
                Code: @"@namespace MudX.Docs.SecurityCode

<MudStack Spacing=""2"">
    <MudStack Row=""true"">
        <MudSwitch @bind-Value=""_disabled"" Color=""Color.Primary"">Disabled</MudSwitch>
        <MudSwitch @bind-Value=""_error"" Color=""Color.Error"">Error</MudSwitch>
    </MudStack>

    <MudXSecurityCode @bind-Code=""_code""
                      Label=""Verification code""
                      HelperText=""Enter the four-digit code from your authenticator.""
                      Required=""true""
                      Disabled=""_disabled""
                      Error=""_error""
                      ErrorText=""The verification code is invalid.""
                      AriaLabel=""Account verification code"" />
</MudStack>

@code {
    private string? _code;
    private bool _disabled;
    private bool _error;
}
",
                Language: CodeLanguage.Razor
            )
        };
    }
}
