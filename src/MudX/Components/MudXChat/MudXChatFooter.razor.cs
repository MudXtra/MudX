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
        /// The time to display within this header.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Chat.Appearance)]
        public string? Text { get; set; }

        /// <summary>
        /// Child content of component.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Chat.Behavior)]
        public RenderFragment? ChildContent { get; set; }
    }
}
