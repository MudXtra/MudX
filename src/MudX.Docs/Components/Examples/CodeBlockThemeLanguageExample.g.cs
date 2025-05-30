using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class CodeBlockThemeLanguageExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "CodeBlockThemeLanguageExample.razor",
                Code: @"@namespace MudX.Docs.CodeBlock

<MudGrid>
    <MudItem xs=""12"" md=""4"">
        <MudSwitch @bind-Value=""@_invisibles"" Color=""Color.Primary"">Invisibles</MudSwitch>
    </MudItem>
    <MudItem xs=""12"" md=""4"">
        <MudSwitch @bind-Value=""@_matchcase"" Color=""Color.Primary"">Match Braces</MudSwitch>
    </MudItem>
    <MudItem xs=""12"" md=""4"">
        <MudSwitch @bind-Value=""@_lineNumbers"" Color=""Color.Primary"">Line Numbers</MudSwitch>
    </MudItem>
    <MudItem xs=""12"" md=""12"">
        <MudAutocomplete T=""CodeTheme"" Strict=""false"" SearchFunc=""@SearchCodeTheme"" @bind-Value=""@_selectedTheme"" Label=""Selected Code Theme"" />
    </MudItem>
</MudGrid>

<MudXCodeBlock Codes=""@_codes"" Theme=""@_selectedTheme"" LineNumbers=""@_lineNumbers"" Invisibles=""@_invisibles"" MatchBraces=""@_matchcase"" />

@code {
    private bool _lineNumbers = false;
    private bool _invisibles = false;
    private bool _matchcase = true;
    private CodeTheme _selectedTheme = CodeTheme.Default;

    private readonly List<CodeFile> _codes = [];

    protected override void OnInitialized()
    {
        _codes.AddRange(GenerateAllLanguageSnippets());
        base.OnInitialized();
    }

    private async Task<IEnumerable<CodeTheme>> SearchCodeTheme(string value, CancellationToken token)
    {
        await Task.CompletedTask;
        // get a list of CodeTheme enum values
        var list = Enum.GetValues(typeof(CodeTheme)).Cast<CodeTheme>().OrderBy(x => x.ToString()).ToList();

        if (string.IsNullOrEmpty(value))
        {
            return list;
        }
        else
        {
            return list.Where(x => x.ToString().ToLower().Contains(value.Trim().ToLower()));
        }
    }

    private IEnumerable<CodeFile> GenerateAllLanguageSnippets() => new[]
    {
        new CodeFile(""Html.html"", ""<!DOCTYPE html>\n<html>\n  <body>Hello</body>\n</html>"", CodeLanguage.HTML),
        new CodeFile(""Xml.xml"", ""<note>\n  <to>User</to>\n</note>"", CodeLanguage.HTML),
        new CodeFile(""Markup.markup"", ""<custom-tag>Value</custom-tag>"", CodeLanguage.HTML),
        new CodeFile(""Svg.svg"", ""<svg><circle cx=\""50\"" cy=\""50\"" r=\""40\"" /></svg>"", CodeLanguage.HTML),
        new CodeFile(""MathML.mathml"", ""<math><mi>x</mi><mo>=</mo><mn>5</mn></math>"", CodeLanguage.HTML),
        new CodeFile(""SSML.ssml"", ""<speak>Hello there!</speak>"", CodeLanguage.HTML),
        new CodeFile(""Atom.atom"", ""<feed xmlns=\""http://www.w3.org/2005/Atom\""></feed>"", CodeLanguage.HTML),
        new CodeFile(""Rss.rss"", ""<rss><channel><title>Feed</title></channel></rss>"", CodeLanguage.HTML),
        new CodeFile(""Css.css"", ""body { color: blue; }"", CodeLanguage.CSS),
        new CodeFile(""C.c"", ""int main() { return 0; }"", CodeLanguage.Clike),
        new CodeFile(""JavaScript.js"", ""console.log('Hello');"", CodeLanguage.JavaScript),
        new CodeFile(""Dockerfile"", ""FROM node:18\nCMD [\""node\""]"", CodeLanguage.Docker),
        new CodeFile(""Markdown.md"", ""# Hello Markdown"", CodeLanguage.Markdown),
        new CodeFile(""Data.json"", ""{ \""key\"": \""value\"" }"", CodeLanguage.JSON),
        new CodeFile(""Java.java"", ""public class Main { public static void main(String[] args) {} }"", CodeLanguage.Java),
        new CodeFile(""Go.go"", ""package main\nfunc main() {}"", CodeLanguage.Go),
        new CodeFile(""Script.ps1"", ""Write-Output 'Hello'"", CodeLanguage.Powershell),
        new CodeFile(""Script.py"", ""print(\""Hello\"")"", CodeLanguage.Python),
        new CodeFile(""App.ts"", ""let msg: string = 'Hi';"", CodeLanguage.TypeScript),
        new CodeFile(""App.php"", ""<?php echo 'Hi'; ?>"", CodeLanguage.PHP),
        new CodeFile(""Query.mongo"", ""db.users.find({})"", CodeLanguage.Mongodb),
        new CodeFile(""Program.cs"", ""Console.WriteLine(\""Hi\"");"", CodeLanguage.CSharp),
        new CodeFile(""WebForm.aspx"", ""<%@ Page Language=\""C#\"" %>"", CodeLanguage.Aspnet),
        new CodeFile(""View.razor"", ""@code { string message = \""Hello\""; }"", CodeLanguage.Razor),
        new CodeFile(""Script.rb"", ""puts 'Hi'"", CodeLanguage.Ruby),
        new CodeFile(""Query.sql"", ""SELECT * FROM users;"", CodeLanguage.SQL),
        new CodeFile(""Config.yaml"", ""name: example\nversion: 1.0"", CodeLanguage.YAML)
    };
}
",
                Language: CodeLanguage.Razor
            )
        };
    }
}
