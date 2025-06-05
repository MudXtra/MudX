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

<style>
    .set-text {
        max-width: 200px;
    }

        .set-text input {
            text-align: center;
        }
</style>
<MudStack>
    <MudStack Row AlignItems=""AlignItems.Center"" Justify=""Justify.SpaceAround"" Breakpoint=""Breakpoint.SmAndDown"">
        <MudSwitch Label=""Horizontal"" @bind-Value=""_horizontal"" />
        <MudRadioGroup @bind-Value=""_margin"">
            <MudText>Margin:&nbsp;&nbsp;</MudText>
            <MudRadio Label=""Normal"" Value=""Margin.Normal"" />
            <MudRadio Label=""Dense"" Value=""Margin.Dense"" />
        </MudRadioGroup>
        <MudNumericField Label=""Spacing"" Class=""set-text"" @bind-Value=""_spacing"" Min=""0"" Max=""20"" Step=""1"" />
    </MudStack>

    <MudXSecurityCode Class=""mx-auto"" @bind-Code=""_code""
                      Horizontal=""@_horizontal"" Spacing=""@_spacing"" Margin=""@_margin"" />

    <MudText Class=""mx-auto"" Color=""Color.Info"">
        Security Code: @_code
    </MudText>
</MudStack>

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
