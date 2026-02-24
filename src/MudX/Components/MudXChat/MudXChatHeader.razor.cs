using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Utilities;

namespace MudX
{
    /// <summary>
    /// Represents the header of a <see cref="MudXChat"/>.
    /// </summary>
    public partial class MudXChatHeader : MudComponentBase
    {
        /// <inheritdoc />
        protected string Classname => new CssBuilder("mudx-chat-header")
            .AddClass(Class)
            .Build();

        /// <summary>
        /// The name to display within this header.
        /// </summary>
        [Parameter]
        public string? Name { get; set; }

        /// <summary>
        /// The time to display within this header.
        /// </summary>
        [Parameter]
        public string? Time { get; set; }

        /// <summary>
        /// Child content of component.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }
    }
}
