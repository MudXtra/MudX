using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class ChatBasicExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "ChatBasicExample.razor",
                Code: @"@namespace MudX.Docs.Chat

<MudXChat ChatPosition=""ChatBubblePosition.Start"">
    <MudXChatBubble>
        It's over Anakin
    </MudXChatBubble>
    <MudXChatBubble>
        I have the high ground.
    </MudXChatBubble>
</MudXChat>
<MudXChat ChatPosition=""ChatBubblePosition.End"">
    <MudXChatBubble>
        You underestimate my power!
    </MudXChatBubble>
</MudXChat>
",
                Language: CodeLanguage.Razor
            )
        };
    }
}
