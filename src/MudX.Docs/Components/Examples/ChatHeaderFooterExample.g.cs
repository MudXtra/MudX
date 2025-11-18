using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class ChatHeaderFooterExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "ChatHeaderFooterExample.razor",
                Code: @"@namespace MudX.Docs.Chat

<MudXChat ChatPosition=""ChatBubblePosition.Start"">
    <MudXChatHeader Name=""Obi-Wan Kenobi"" Time=""2 hours ago"" />
    <MudXChatBubble>You were my brother Anakin.</MudXChatBubble>
    <MudXChatFooter Text=""Seen"" />
</MudXChat>

<MudXChat ChatPosition=""ChatBubblePosition.Start"">
    <MudXChatHeader>
        <MudAlert Severity=""Severity.Info"" Dense=""true"">Obi-Wan Kenobi</MudAlert>
    </MudXChatHeader>
    <MudXChatBubble>I loved you.</MudXChatBubble>
    <MudXChatFooter>
        <MudAlert Severity=""Severity.Info"" Dense=""true"">Seen</MudAlert>
    </MudXChatFooter>
</MudXChat>
",
                Language: CodeLanguage.Razor
            )
        };
    }
}
