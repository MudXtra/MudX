using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class BreadcrumbsMaxItemsExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "BreadcrumbsMaxItemsExample.razor",
                Code: @"@namespace MudX.Docs.Breadcrumbs

<MudXBreadcrumbs MaxItems=""@((byte)_maxItems)"" ExpanderIcon=""@_icon"" />

<MudStack Row Breakpoint=""Breakpoint.Xs"" AlignItems=""AlignItems.End"" Justify=""Justify.SpaceEvenly"" Class=""mt-2 mb-2"">
    <div style=""min-width: 200px;"">
        <MudSelect T=""string"" Label=""Max Items Icon"" @bind-Value=""_icon"">
            <MudSelectItem Value=""@Icons.Material.Filled.SettingsEthernet"">SettingsEthernet</MudSelectItem>
            <MudSelectItem Value=""@Icons.Material.Filled.Adjust"">Adjust</MudSelectItem>
            <MudSelectItem Value=""@Icons.Material.Filled.ArrowForwardIos"">ArrowForwardIos</MudSelectItem>
            <MudSelectItem Value=""@Icons.Material.Filled.ArrowRight"">ArrowRight</MudSelectItem>
        </MudSelect>
    </div>
    <div>
        
        <MudSlider T=""int"" ValueLabel=""true"" @bind-Value=""_maxItems"" Min=""1"" Max=""5""
                   TickMarks=""true"" TickMarkLabels=""@_labels"">
            <MudText Typo=""Typo.caption"">Max Items</MudText>
        </MudSlider>       
    </div>    
</MudStack>

@code {
    private int _maxItems = 2;
    private string[] _labels = [ ""1"", ""2"", ""3"", ""4"", ""5"" ];
    private string _icon = Icons.Material.Filled.SettingsEthernet;
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
