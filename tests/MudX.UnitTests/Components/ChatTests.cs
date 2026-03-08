using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using NUnit.Framework;

namespace MudX.UnitTests.Components
{
    [TestFixture]
    public class ChatTests : BunitTest
    {
        [Test]
        public void MudXChat_DefaultValues()
        {
            var comp = Context.RenderComponent<MudXChat>();
            comp.Instance.Color.Should().Be(Color.Default);
            comp.Instance.ChatPosition.Should().Be(ChatBubblePosition.Start);
            comp.Instance.Elevation.Should().Be(0);
            comp.Instance.Square.Should().Be(false);
            comp.Instance.Dense.Should().Be(false);
            comp.Instance.Variant.Should().Be(Variant.Text);
        }

        [Test]
        public void MudXChat_CssClasses()
        {
            var comp = Context.RenderComponent<MudXChat>(parameters => parameters
                .Add(p => p.ChatPosition, ChatBubblePosition.End)
                .Add(p => p.ArrowPosition, ChatArrowPosition.Middle)
                .Add(p => p.Square, true)
                .Add(p => p.Dense, true)
                .Add(p => p.Elevation, 2)
                .Add(p => p.Class, "custom-class"));

            comp.Markup.Should().Contain("mudx-chat-end");
            comp.Markup.Should().Contain("mud-square");
            comp.Markup.Should().Contain("mud-dense");
            comp.Markup.Should().Contain("mud-elevation-2");
            comp.Markup.Should().Contain("custom-class");
            comp.Markup.Should().Contain("mudx-chat-arrow-middle");
        }

        [Test]
        public void MudXChatBubble_CssClasses()
        {
            var comp = Context.RenderComponent<MudXChatBubble>(parameters => parameters
                 .Add(p => p.Color, Color.Success)
                 .Add(p => p.Variant, Variant.Outlined));

            comp.Markup.Should().Contain("mudx-chat-bubble");
            comp.Markup.Should().Contain("mudx-chat-outlined-success");
            comp.Markup.Should().Contain("mudx-chat-arrow-none");
        }

        [Test]
        public void MudXChatBubble_InheritsParentValues()
        {
            var comp = Context.RenderComponent<MudXChat>(parameters => parameters
                .Add(p => p.Color, Color.Primary)
                .Add(p => p.Variant, Variant.Filled)
                .Add(p => p.ArrowPosition, ChatArrowPosition.Middle)
                .Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<MudXChatBubble>(0);
                    builder.CloseComponent();
                }));

            var bubble = comp.FindComponent<MudXChatBubble>();
            bubble.Instance.ParentColor.Should().Be(Color.Primary);
            bubble.Instance.ParentVariant.Should().Be(Variant.Filled);
            bubble.Instance.ParentArrowPosition.Should().Be(ChatArrowPosition.Middle);
            bubble.Markup.Should().Contain("mudx-chat-filled-primary");
        }

        [Test]
        public void MudXChatBubble_HasElementReference()
        {
            var comp = Context.RenderComponent<MudXChatBubble>();
            var elementRef = comp.Instance.ElementReference;
            elementRef.Should().NotBeNull();
        }

        [Test]
        public void MudXChatBubble_OverridesParentValues()
        {
            var comp = Context.RenderComponent<MudXChat>(parameters => parameters
                .Add(p => p.Color, Color.Primary)
                .Add(p => p.Variant, Variant.Filled)
                .Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<MudXChatBubble>(0);
                    builder.AddAttribute(1, "Color", Color.Secondary);
                    builder.AddAttribute(2, "Variant", Variant.Outlined);
                    builder.CloseComponent();
                }));

            var bubble = comp.FindComponent<MudXChatBubble>();
            bubble.Markup.Should().Contain("mudx-chat-outlined-secondary");
        }

        [Test]
        public async Task MudXChatBubble_ClickEvents()
        {
            var clicked = false;
            var rightClicked = false;

            var comp = Context.RenderComponent<MudXChatBubble>(parameters => parameters
                .Add(p => p.OnClick, (MouseEventArgs e) => { clicked = true; })
                .Add(p => p.OnContextClick, (MouseEventArgs e) => { rightClicked = true; }));

            await comp.InvokeAsync(() => comp.Instance.OnClickHandler(new MouseEventArgs()));
            clicked.Should().BeTrue();

            await comp.InvokeAsync(() => comp.Instance.OnContextHandler(new MouseEventArgs()));
            rightClicked.Should().BeTrue();
        }

        [Test]
        public void MudXChat_RightToLeft()
        {
            var comp = Context.RenderComponent<MudXChat>(parameters => parameters
                .Add(p => p.RightToLeft, true));

            comp.Markup.Should().Contain("mudx-chat-rtl");
        }

        [Test]
        public void MudXChat_CustomStyles()
        {
            var comp = Context.RenderComponent<MudXChat>(parameters => parameters
                .Add(p => p.Style, "background-color: red;")
                .Add(p => p.Class, "custom-class"));

            comp.Markup.Should().Contain("style=\"background-color: red;\"");
            comp.Markup.Should().Contain("custom-class");
        }

        [Test]
        public void MudXChatHeader_Parameters()
        {
            var comp = Context.RenderComponent<MudXChatHeader>(parameters => parameters
                .Add(p => p.Name, "John Doe")
                .Add(p => p.Time, "12:00 PM")
                .Add(p => p.Class, "custom-header-class"));

            comp.Markup.Should().Contain("mudx-chat-header");
            comp.Markup.Should().Contain("John Doe");
            comp.Markup.Should().Contain("12:00 PM");
            comp.Markup.Should().Contain("custom-header-class");
        }

        [Test]
        public void MudXChatHeader_ChildContent()
        {
            var comp = Context.RenderComponent<MudXChatHeader>(parameters => parameters
                .Add(p => p.ChildContent, builder =>
                {
                    builder.AddContent(0, "Custom Header Content");
                }));

            comp.Markup.Should().Contain("Custom Header Content");
        }

        [Test]
        public void MudXChatFooter_Parameters()
        {
            var comp = Context.RenderComponent<MudXChatFooter>(parameters => parameters
                .Add(p => p.Text, "Typing...")
                .Add(p => p.Class, "custom-footer-class"));

            comp.Markup.Should().Contain("mudx-chat-footer");
            comp.Markup.Should().Contain("Typing...");
            comp.Markup.Should().Contain("custom-footer-class");
        }

        [Test]
        public void MudXChatFooter_ChildContent()
        {
            var comp = Context.RenderComponent<MudXChatFooter>(parameters => parameters
                .Add(p => p.ChildContent, builder =>
                {
                    builder.AddContent(0, "Custom Footer Content");
                }));

            comp.Markup.Should().Contain("Custom Footer Content");
        }

    }
}
