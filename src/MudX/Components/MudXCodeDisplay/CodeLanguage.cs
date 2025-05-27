using System.ComponentModel;

namespace MudX
{
    /// <summary>
    /// Supported Code Languages for Prism.js Syntax Highlighting
    /// </summary>
    public enum CodeLanguage
    {
        /// <summary>
        /// Cascading Style Sheets
        /// </summary>
        [Description("css")]
        CSS,
        /// <summary>
        /// HTML, XML, Markup, SVG, MathML, SSML, Atom, RSS
        /// </summary>
        [Description("html")]
        HTML,
        /// <summary>
        /// Pure C# for Syntax Highlighting, might also consider aspnet c# or razor c#
        /// </summary>
        [Description("csharp")]
        CSharp,
        /// <summary>
        /// C like language for Syntax Highlighting
        /// </summary>
        [Description("clike")]
        Clike,
        /// <summary>
        /// JavaScript
        /// </summary>
        [Description("javascript")]
        JavaScript,
        /// <summary>
        /// JSON
        /// </summary>
        [Description("json")]
        JSON,
        /// <summary>
        /// Docker
        /// </summary>
        [Description("docker")]
        Docker,
        /// <summary>
        /// Markdown
        /// </summary>
        [Description("markdown")]
        Markdown,
        /// <summary>
        /// Java
        /// </summary>
        [Description("java")]
        Java,
        /// <summary>
        /// Python
        /// </summary>
        [Description("python")]
        Python,
        /// <summary>
        /// SQL
        /// </summary>
        [Description("sql")]
        SQL,
        /// <summary>
        /// Go
        /// </summary>
        [Description("go")]
        Go,
        /// <summary>
        /// Powershell
        /// </summary>
        [Description("powershell")]
        Powershell,
        /// <summary>
        /// TypeScript
        /// </summary>
        [Description("typescript")]
        TypeScript,
        /// <summary>
        /// PHP
        /// </summary>
        [Description("php")]
        PHP,
        /// <summary>
        /// Ruby
        /// </summary>
        [Description("ruby")]
        Ruby,
        /// <summary>
        /// YAML
        /// </summary>
        [Description("yaml")]
        YAML,
        /// <summary>
        /// Razor C# for .razor and .cshtml
        /// </summary>
        [Description("razor")]
        Razor,
        /// <summary>
        /// Classical ASP.NET C# 
        /// </summary>
        [Description("aspnet")]
        Aspnet,
        /// <summary>
        /// MongoDB
        /// </summary>
        [Description("mongodb")]
        Mongodb
    }
}
