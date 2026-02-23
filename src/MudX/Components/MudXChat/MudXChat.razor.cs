using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Utilities;
using MudX.Extensions;

namespace MudX
{
    /// <summary>
    /// The MudXChat component is used to house one or more MudChatBubble components, with optional components such as MudAvatar, MudChatHeader, and MudChatFooter.
    /// </summary>
    public partial class MudXChat : MudComponentBase
    {
        /// <inheritdoc />
        protected string Classname => new CssBuilder("mudx-chat")
            .AddClass($"mudx-chat-{ChatPosition.ToDescription()}")
            .AddClass($"mudx-chat-arrow-{ArrowPosition.ToDescription()}")
            .AddClass($"mud-square", Square)
            .AddClass($"mudx-chat-rtl", RightToLeft)
            .AddClass($"mud-dense", Dense)
            .AddClass($"mud-elevation-{Elevation}")
            .AddClass(Class)
            .Build();

        /// <summary>
        /// Whether or not to use Right To Left
        /// </summary>
        [CascadingParameter(Name = "RightToLeft")]
        public bool RightToLeft { get; private set; }

        /// <summary>
        /// Child chat bubbles default color, can be overridden by bubble.
        /// </summary>
        [Parameter]
        public Color Color { get; set; } = Color.Default;

        /// <summary>
        /// Display variant to use.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Variant.Text" />. The variant changes the appearance of the chat bubbles, such as <c>Text</c>, <c>Outlined</c>, or <c>Filled</c>.
        /// </remarks>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Text;

        /// <summary>
        /// Chat bubble position.
        /// </summary>
        [Parameter]
        public ChatBubblePosition ChatPosition { get; set; } = ChatBubblePosition.Start;

        /// <summary>
        /// The Chat Bubble Arrow Position.
        /// </summary>
        /// <remarks>Defaults to Top</remarks>
        [Parameter]
        public ChatArrowPosition ArrowPosition { get; set; } = ChatArrowPosition.Top;

        /// <summary>
        /// Child content of component.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Size of the drop shadow.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>0</c>.  A higher number creates a heavier drop shadow.  Use a value of <c>0</c> for no shadow.
        /// </remarks>
        [Parameter]
        [Category(CategoryTypes.Alert.Appearance)]
        public int Elevation { set; get; } = 0;

        /// <summary>
        /// Gets or sets whether rounded corners are disabled.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>false</c>.
        /// </remarks>
        [Parameter]
        [Category(CategoryTypes.Alert.Appearance)]
        public bool Square { get; set; } = false;

        /// <summary>
        /// Gets or sets whether compact padding will be used.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>false</c>.
        /// </remarks>
        [Parameter]
        [Category(CategoryTypes.Alert.Appearance)]
        public bool Dense { get; set; }
    }
}
