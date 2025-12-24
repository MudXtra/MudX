using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Utilities;
using MudX.Extensions;

namespace MudX
{
    /// <summary>
    /// Represents the content displayed within a <see cref="MudXChat"/>.
    /// </summary>
    public partial class MudXChatBubble : MudComponentBase
    {
        private Color GetBubbleColor => Color != null ? Color.Value : ParentColor;
        private Variant GetBubbleVariant => Variant != null ? Variant.Value : ParentVariant;

        /// <inheritdoc />
        protected string Classname => new CssBuilder("mudx-chat-bubble")
            .AddClass($"mudx-chat-{GetBubbleVariant.ToDescriptionString()}")
            .AddClass($"mudx-chat-{GetBubbleVariant.ToDescriptionString()}-{GetBubbleColor.ToDescriptionString()}")
            .AddClass($"mudx-chat-arrow-{ParentArrowPosition.ToDescription()}")
            .AddClass("mudx-chat-bubble-clickable", OnClick.HasDelegate || OnContextClick.HasDelegate)
            .AddClass("mud-ripple", OnClick.HasDelegate || OnContextClick.HasDelegate)
            .AddClass(Class)
            .Build();

        /// <summary>
        /// The containing div Element Reference
        /// </summary>
        public ElementReference ElementReference { get; private set; }

        /// <summary>
        /// The variant provided by the <see cref="MudXChat" /> component.
        /// </summary>
        [CascadingParameter(Name = "MudChatBubbleVariant")]
        public Variant ParentVariant { get; private set; }

        /// <summary>
        /// The color provided by the <see cref="MudXChat" /> component.
        /// </summary>
        [CascadingParameter(Name = "MudChatBubbleColor")]
        public Color ParentColor { get; private set; }

        /// <summary>
        /// The arrow position provided by the <see cref="MudXChat" /> component.
        /// </summary>
        [CascadingParameter(Name = "MudChatArrowPosition")]
        public ChatArrowPosition ParentArrowPosition { get; private set; } = ChatArrowPosition.None;

        /// <summary>
        /// The color of the component. It supports the theme colors.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Chat.Appearance)]
        public Color? Color { get; set; }

        /// <summary>
        /// The display variant of the component (e.g., filled, outlined).
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Chat.Appearance)]
        public Variant? Variant { get; set; }

        /// <summary>
        /// Child content of component.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Chat.Behavior)]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Occurs when the chat bubble has been clicked.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Chat.Behavior)]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        /// <summary>
        /// Occurs when the chat bubble has been right-clicked.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.Chat.Behavior)]
        public EventCallback<MouseEventArgs> OnContextClick { get; set; }

        /// <summary>
        /// Internal handler invoked when the chat bubble is clicked; forwards the event to the <see cref="OnClick"/> cakllback if it has been set.
        /// </summary>
        internal async Task OnClickHandler(MouseEventArgs mouseEventArgs)
        {
            if (OnClick.HasDelegate)
            {
                await OnClick.InvokeAsync(mouseEventArgs);
            }
        }

        /// <summary>
        /// Internal handler invoked when the chat bubble is right-clicked; forwards the event to the <see cref="OnContextClick"/> 
        /// callback if it has been set.
        /// </summary>
        internal async Task OnContextHandler(MouseEventArgs mouseEventArgs)
        {
            if (OnContextClick.HasDelegate)
            {
                await OnContextClick.InvokeAsync(mouseEventArgs);
            }
        }
    }

}
