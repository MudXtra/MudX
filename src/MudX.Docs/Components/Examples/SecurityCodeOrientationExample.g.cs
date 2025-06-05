using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class SecurityCodeOrientationExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "SecurityCodeOrientationExample.razor",
                Code: @"@namespace MudX.Docs.SecurityCode

<MudGrid>
    <MudItem xs=""12"" xl=""4"">
        <MudSwitch Label=""Horizontal"" @bind-Value=""_horizontal"" />
    </MudItem>
    <MudItem xs=""12"" xl=""4"">
        <MudRadioGroup @bind-Value=""_margin"">
            <MudRadio Label=""Normal"" Value=""Margin.Normal"" />
            <MudRadio Label=""Dense"" Value=""Margin.Dense"" />
        </MudRadioGroup>
    </MudItem>
    <MudItem xs=""12"" xl=""4"">
        <MudNumericField Label=""Spacing"" @bind-Value=""_spacing"" Min=""0"" Max=""20"" Step=""1""/>
    </MudItem>
</MudGrid>

<MudXSecurityCode Class=""d-flex mx-auto"" @bind-Code=""_code"" Horizontal=""@_horizontal"" Spacing=""@_spacing"" />

<MudText Class=""d-flex mx-auto"" Color=""Color.Info"">
    Security Code: @_code
</MudText>

@code {
    private string? _code;
    private bool _horizontal = true;
    private Margin _margin = Margin.Normal;
    private int _spacing = 2;
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
