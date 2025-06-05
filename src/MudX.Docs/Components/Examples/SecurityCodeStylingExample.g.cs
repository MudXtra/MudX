using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class SecurityCodeStylingExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "SecurityCodeStylingExample.razor",
                Code: @"@namespace MudX.Docs.SecurityCode

<MudGrid>
    <MudRadioGroup @bind-Value=""_variant"">
        <MudText>Editable Variant</MudText>
        <MudRadio Label=""Text"" Value=""Variant.Text"" />
        <MudRadio Label=""Outlined"" Value=""Variant.Outlined"" />
        <MudRadio Label=""Filled"" Value=""Variant.Filled"" />
    </MudRadioGroup>
    <MudItem xs=""12"" xl=""4"">
        <MudRadioGroup @bind-Value=""_readonlyVariant"">
            <MudText>Readonly Variant</MudText>
            <MudRadio Label=""Text"" Value=""Variant.Text"" />
            <MudRadio Label=""Outlined"" Value=""Variant.Outlined"" />
            <MudRadio Label=""Filled"" Value=""Variant.Filled"" />
        </MudRadioGroup>
    </MudItem>
    <MudItem xs=""12"" xl=""4"">
        <MudSwitch @bind-Value=""_underline"" Label=""Underline"" />
    </MudItem>
    <MudItem xs=""12"" xl=""4"">
        <MudSwitch @bind-Value=""_password"" Label=""Password"" />
    </MudItem>
</MudGrid>

<MudXSecurityCode Class=""d-flex mx-auto"" @bind-Code=""_code"" 
                  Variant=""_variant"" ReadonlyVariant=""_readonlyVariant"" 
                  Password=""_password"" Underline=""_underline"" />

<MudText Class=""d-flex mx-auto"" Color=""Color.Info"">
    Security Code: @_code
</MudText>

@code {
    private string? _code;
    private Variant _readonlyVariant = Variant.Text;
    private Variant _variant = Variant.Outlined;
    private bool _underline = false;
    private bool _password = false;
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
