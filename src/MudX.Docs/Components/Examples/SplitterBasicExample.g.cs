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

<MudPaper Class=""pa-4"" Elevation=""0"">
    <MudXSplitter>
        <StartSplitter>
            <MudImage ObjectFit=""ObjectFit.ScaleDown"" Fluid ObjectPosition=""ObjectPosition.Center"" Class=""rounded"" Src=""./_content/MudX.Docs/images/mudx_logo.png"" />
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
