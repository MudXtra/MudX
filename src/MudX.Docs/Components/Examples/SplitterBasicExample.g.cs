using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class SplitterBasicExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "SplitterBasicExample.razor",
                Code: @"@namespace MudX.Docs.Splitter

<style>
    .basic-splitter {
        min-height: 100px;
        max-height: 300px;
    }
</style>

<MudPaper Class=""pa-4"">
    <MudXSplitter Class=""basic-splitter"">
        <StartSplitter>
            <MudImage Fluid Src=""./_content/MudX.Docs/images/mudx_logo.png"" Style=""height: auto;width: 100%"" />
        </StartSplitter>
        <EndSplitter>
            This is the right side of the splitter, showing that text and images work side by side with the MudXSplitter component. Smooth dragging!
        </EndSplitter>
    </MudXSplitter>
</MudPaper>

@code {

}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
