using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class OutlineBasicExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "OutlineBasicExample.razor",
                Code: @"@namespace MudX.Docs.Outline

<MudXOutline ScrollContainerSelector=""@($""#{ContentId}"")"">
    <MudXOutlineSection Title=""Item 1"">
        <MudText Typo=""Typo.h6"">Item 1</MudText>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
        <br /><br /><br /><br /><br />
    </MudXOutlineSection>
    <MudXOutlineSection Title=""Item 2"">
        <MudText Typo=""Typo.h6"">Item 2</MudText>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
        <br /><br /><br /><br /><br />
    </MudXOutlineSection>
    <MudXOutlineSection Title=""Item 3"">
        <MudText Typo=""Typo.h6"">Item 3</MudText>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
        <br /><br /><br /><br /><br />
    </MudXOutlineSection>
</MudXOutline>

@code {
    /// <summary>
    /// The id of the scrollable container
    /// </summary>
    [CascadingParameter(Name = ""ContentId"")]
    public string ContentId { get; set; } = default!;
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
