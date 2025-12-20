using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class ChatAvatarExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "ChatAvatarExample.razor",
                Code: @"@namespace MudX.Docs.Chat

<MudXChat ChatPosition=""ChatBubblePosition.Start"">
    <MudAvatar>OK</MudAvatar>
    <MudXChatBubble>
        It was said that you would, destroy the Sith, not join them.
    </MudXChatBubble>
    <MudXChatBubble>
        It was you who would bring balance to the Force
    </MudXChatBubble>
</MudXChat>

<MudXChat ChatPosition=""ChatBubblePosition.Start"">
    <MudAvatar>
        <MudImage Src=""./_content/MudX.Docs/images/jonny.jpg"" />
    </MudAvatar>
    <MudXChatBubble>
        Not leave it in Darkness
    </MudXChatBubble>
</MudXChat>
",
                Language: CodeLanguage.Razor
            )
        };
    }
}
