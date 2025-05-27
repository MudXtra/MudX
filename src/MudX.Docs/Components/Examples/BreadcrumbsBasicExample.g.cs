using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class BreadcrumbsBasicExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "BreadcrumbsBasicExample.razor",
                Code: @"@namespace MudX.Docs.Breadcrumbs

<MudXBreadcrumbs />",
                Language: CodeLanguage.Razor
            )
        };
    }
}
