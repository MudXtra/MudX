using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Utilities;

namespace MudX
{
    /// <summary>
    /// Represents the footer of a <see cref="MudXChat"/>.
    /// </summary>
    public partial class MudXChatFooter : MudComponentBase
    {
        /// <inheritdoc />
        protected string Classname => new CssBuilder("mudx-chat-footer")
            .AddClass(Class)
            .Build();

        /// <summary>
        /// The time to display within this footer.
        /// </summary>
        [Parameter]
        public string? Text { get; set; }

        /// <summary>
        /// Child content of component.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }
    }
}
