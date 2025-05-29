using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class CodeBlockCustomizedExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "CodeBlockCustomizedExample.razor",
                Code: @"@namespace MudX.Docs.CodeBlock

<MudGrid>
    <MudItem xs=""12"" sm=""9"" md=""6"" lg=""4"" xl=""2"">
        <MudButton Variant=""Variant.Filled"" Color=""Color.Primary"" OnClick=""AddCode"">Add CodeFile</MudButton>
    </MudItem>
    <MudItem xs=""12"" sm=""9"" md=""6"" lg=""4"" xl=""2"">
        <MudSwitch @bind-Value=""@_invisibles"" Color=""Color.Primary"">Invisibles</MudSwitch>
    </MudItem>
    <MudItem xs=""12"" sm=""9"" md=""6"" lg=""4"" xl=""2"">
        <MudSwitch @bind-Value=""@_matchcase"" Color=""Color.Primary"">Match Case</MudSwitch>
    </MudItem>
    <MudItem xs=""12"" sm=""9"" md=""6"" lg=""4"" xl=""2"">
        <MudSwitch @bind-Value=""@_lineNumbers"" Color=""Color.Primary"">Line Numbers</MudSwitch>
    </MudItem>
</MudGrid>


<MudXCodeBlock Codes=""@_codes"" />

@code {
    private bool _lineNumbers = false;
    private bool _invisibles = false;
    private bool _matchcase = true;
    private int _counter = 0;
    private IEnumerable<CodeFile> _codes = [];

    private void AddCode() 
    {
        _counter++;
        _codes = _codes.Append(GenerateCodeFile(_counter));
    }

    protected override void OnInitialized()
    {
        AddCode();
        base.OnInitialized();
    }

    private CodeFile GenerateCodeFile(int counter) 
    {
        return new CodeFile(
            Title: $""Main{counter}.cs"",
            Language: CodeLanguage.CSharp,
            Code: 
            ""public class Program\r\n"" +
            ""{\t\\\\ Counter: "" + counter + ""\r\n"" +            
            ""    static void Main(string[] args)\r\n"" +
            ""    {\r\n"" +
            ""        Console.WriteLine(\""Hello World!\"");\r\n"" +
            ""    }\r\n"" +
            ""}""
        );
    }
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
