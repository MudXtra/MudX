using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class BreadcrumbsSettingsExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "BreadcrumbsSettingsExample.razor",
                Code: @"@namespace MudX.Docs.Breadcrumbs

<div @key=_counter>
    @if (string.IsNullOrWhiteSpace(_separator))
    {
        <MudXBreadcrumbs>
            <SeparatorTemplate>
                <MudIcon Icon=""@_icon"" />
            </SeparatorTemplate>
        </MudXBreadcrumbs>
    }
    else
    {
        <MudXBreadcrumbs Separator=""@_separator"" />
    }
</div>

<MudStack Row Breakpoint=""Breakpoint.Xs"" AlignItems=""AlignItems.Center"" Justify=""Justify.SpaceEvenly"">
    <div style=""min-width: 200px;"">
        <MudSelect T=""string"" Label=""Separator Icon"" @bind-Value=""_icon"" @bind-Value:after=""@(() => _counter++)"">
            <MudSelectItem Value=""@Icons.Material.Filled.SettingsEthernet"">SettingsEthernet</MudSelectItem>
            <MudSelectItem Value=""@Icons.Material.Filled.Adjust"">Adjust</MudSelectItem>
            <MudSelectItem Value=""@Icons.Material.Filled.ArrowForwardIos"">ArrowForwardIos</MudSelectItem>
            <MudSelectItem Value=""@Icons.Material.Filled.ArrowRight"">ArrowRight</MudSelectItem>
        </MudSelect>
    </div>
    <div style=""min-width: 200px;"">
        <MudTextField Label=""Separator"" ShrinkLabel=""true"" Immediate @bind-Value=""_separator"" @bind-Value:after=""@(() => _counter++)"" FullWidth=""false"" MaxLength=""1"" Clearable />
    </div>    
</MudStack>

@code {
    private int _counter = 0;
    private string? _separator;
    private string _icon = Icons.Material.Filled.SettingsEthernet;
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
