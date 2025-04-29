using Microsoft.AspNetCore.Components;

namespace MudX.Docs
{
    public partial class DocsPage
    {

        [Parameter, EditorRequired]
        public required RenderFragment ChildContent { get; set; }
    }
}
