using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MudX.Docs
{
    public partial class DocsPage : MudComponentBase
    {

        [Parameter, EditorRequired]
        public required RenderFragment ChildContent { get; set; }
    }
}
