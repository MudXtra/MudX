using Microsoft.AspNetCore.Components;

namespace MudX.Docs
{
    public partial class DocsPage
    {
        [Parameter, EditorRequired]
        public required RenderFragment ChildContent { get; set; }

        [Parameter, EditorRequired]
        public required RenderFragment ApiContent { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
        }
    }
}
