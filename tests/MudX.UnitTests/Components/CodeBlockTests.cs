using System.Reflection;
using Bunit;
using FluentAssertions;
using MudBlazor;
using MudX.UnitTests.Viewer.TestComponents.CodeBlock;
using MudX.Utilities;
using NUnit.Framework;

namespace MudX.UnitTests.Components
{
    [TestFixture]
    public class CodeBlockTests : BunitTest
    {
        [Test]
        public void CodeTheme_ShouldHaveCorrectDescriptions()
        {
            // Assert: Check each enum member has the correct description
            AssertEnumDescription(CodeTheme.Default, "default");
            AssertEnumDescription(CodeTheme.Okaidia, "okaidia");
            AssertEnumDescription(CodeTheme.Dark, "dark");
            AssertEnumDescription(CodeTheme.Funky, "funky");
            AssertEnumDescription(CodeTheme.Twilight, "twilight");
            AssertEnumDescription(CodeTheme.Coy, "coy");
            AssertEnumDescription(CodeTheme.SolarizedLight, "solarizedlight");
            AssertEnumDescription(CodeTheme.TomorrowNight, "tomorrownight");
            AssertEnumDescription(CodeTheme.MaterialLight, "materiallight");
            AssertEnumDescription(CodeTheme.MaterialDark, "materialdark");
        }

        [Test]
        public void CodeLanguage_ShouldHaveCorrectDescriptions()
        {
            // Assert: Check each enum member has the correct description
            AssertEnumDescription(CodeLanguage.CSS, "css");
            AssertEnumDescription(CodeLanguage.HTML, "html");
            AssertEnumDescription(CodeLanguage.CSharp, "csharp");
            AssertEnumDescription(CodeLanguage.Clike, "clike");
            AssertEnumDescription(CodeLanguage.JavaScript, "javascript");
            AssertEnumDescription(CodeLanguage.JSON, "json");
            AssertEnumDescription(CodeLanguage.Docker, "docker");
            AssertEnumDescription(CodeLanguage.Markdown, "markdown");
            AssertEnumDescription(CodeLanguage.Java, "java");
            AssertEnumDescription(CodeLanguage.Python, "python");
            AssertEnumDescription(CodeLanguage.SQL, "sql");
            AssertEnumDescription(CodeLanguage.Go, "go");
            AssertEnumDescription(CodeLanguage.Powershell, "powershell");
            AssertEnumDescription(CodeLanguage.TypeScript, "typescript");
            AssertEnumDescription(CodeLanguage.PHP, "php");
            AssertEnumDescription(CodeLanguage.Ruby, "ruby");
            AssertEnumDescription(CodeLanguage.YAML, "yaml");
            AssertEnumDescription(CodeLanguage.Razor, "razor");
            AssertEnumDescription(CodeLanguage.Aspnet, "aspnet");
            AssertEnumDescription(CodeLanguage.Mongodb, "mongodb");
        }

        [Test]
        public void CodeFile_ShouldUseProvidedParameters()
        {
            // Arrange: Create a CodeFile
            var codeFile = new CodeFile("Title", "Console.WriteLine(\"Hello\");", CodeLanguage.CSharp);

            // Act & Assert
            codeFile.Title.Should().Be("Title");
            codeFile.Code.Should().Be("Console.WriteLine(\"Hello\");");
            codeFile.Language.Should().Be(CodeLanguage.CSharp);
        }

        [Test]
        public void MudXCodeBlock_ShouldRenderSuccessfully()
        {
            var comp = Context.RenderComponent<CodeBlockBasicTest>();

            // Act: Find elements and execute interactions
            var preElement = comp.Find("pre");

            // Assert: Verify interaction and rendering
            preElement.Should().NotBeNull();
        }

        [Test]
        public void MudXCodeBlock_ShouldApplyThemeCorrectlyAndLoadModule()
        {
            // Arrange: Set up code with a specific theme
            var codeFiles = new List<CodeFile>
            {
                new("Test.cs", "Console.WriteLine(\"Test\");", CodeLanguage.CSharp)
            };

            // Arrange: Setup JSInterop to expect the import and initialize calls
            var jsInterop = Context.JSInterop;

            // Setup the import call to return a mock module
            var moduleMock = jsInterop.SetupModule(AssemblyInfo.ModulePath("mudxPrismWrapper.js"));
            // Setup the initialize call to return true
            moduleMock.Setup<bool>("initialize", _ => true);

            var comp = Context.RenderComponent<MudXCodeBlock>(parameters => parameters
                .Add(p => p.Codes, codeFiles)
                .Add(p => p.Theme, CodeTheme.Dark));

            comp.Instance.PrismCSSPath.Should().Be($"./_content/{AssemblyInfo.PackageId}/prism/prism-dark.css");

            // Assert: Verify the JS module was imported
            jsInterop.VerifyInvoke("import")
                .Arguments[0].Should().Be(AssemblyInfo.ModulePath("mudxPrismWrapper.js"));

            // Assert: Verify the initialize method was called
            moduleMock.VerifyInvoke("initialize");
        }

        private static void AssertEnumDescription<TEnum>(TEnum enumValue, string expectedDescription)
        {
            var memberInfo = typeof(TEnum).GetMember(enumValue!.ToString()!);
            var descriptionAttribute = memberInfo.First().GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false).Cast<System.ComponentModel.DescriptionAttribute>().FirstOrDefault();
            descriptionAttribute.Should().NotBeNull();
            descriptionAttribute!.Description.Should().Be(expectedDescription);
        }

        [Test]
        public void MudXCodeBlock_ShouldRenderCopyButton()
        {
            var provider = Context.RenderComponent<MudPopoverProvider>();
            var comp = Context.RenderComponent<CodeBlockCopyTest>();

            // Act: Find elements and execute interactions
            var preElement = comp.Find("pre");

            // Assert: Verify interaction and rendering
            preElement.Should().NotBeNull();

            provider.Should().NotBeNull();
            var copyButton = provider.Find(".mudx-copy-button button");
            copyButton.Should().NotBeNull();
        }

        [Test]
        public void CodeBlock_ReturnCopyMessage()
        {
            var provider = Context.RenderComponent<MudPopoverProvider>();
            var comp = Context.RenderComponent<CodeBlockCopyTest>();

            // Act: Find elements and execute interactions
            var preElement = comp.Find("pre");

            // Assert: Verify interaction and rendering
            preElement.Should().NotBeNull();

            provider.Should().NotBeNull();
            var copyButton = provider.Find(".mudx-copy-button button");
            copyButton.Should().NotBeNull();
            copyButton.Click();
            // will fail since no JsInterop in bUnit
            provider.WaitForAssertion(() => provider.FindAll(".mud-alert.mud-alert-text-error").Count.Should().Be(1));
            provider.FindAll(".mudx-code-alert").Count.Should().Be(1);
        }

        [Test]
        public void Classname_ShouldContainExpectedClasses()
        {
            var codeFiles = new[] { new CodeFile("Test", "code", CodeLanguage.CSharp) };
            var comp = Context.RenderComponent<MudXCodeBlock>(p =>
                p.Add(x => x.Codes, codeFiles)
                 .Add(x => x.Class, "custom-class"));
            var div = comp.Find("div.mudx-code-display");
            div.Should().NotBeNull();
            div = comp.Find("div.mudx-code-display.custom-class");
            div.Should().NotBeNull();
        }

        [Test]
        public void Stylename_ShouldContainStyle_WhenSet()
        {
            var codeFiles = new[] { new CodeFile("Test", "code", CodeLanguage.CSharp) };
            var comp = Context.RenderComponent<MudXCodeBlock>(p => p
                .Add(x => x.Codes, codeFiles)
                .Add(x => x.Style, "color: red;"));
            var div = comp.Find("div.mudx-code-display");
            div.Should().NotBeNull();
            div.GetAttribute("style").Should().Contain("color: red;");
        }

        [Test]
        public void CodeBlock_ShouldSwitchToTabs_MultipleCodeFiles()
        {
            var comp1 = Context.RenderComponent<CodeBlockBasicTest>();
            var comp2 = Context.RenderComponent<CodeBlockThemeLanguageTest>();
            comp1.FindAll("div.mud-tabs").Count.Should().Be(0);
            comp2.FindAll("div.mud-tabs").Count.Should().Be(1);
            comp2.FindAll("div.mud-tab").Count.Should().Be(27);
        }

        [Test]
        public void CodeBlock_ShouldToggle_Line_Invisibles_Braces()
        {
            var comp = Context.RenderComponent<CodeBlockCustomizedTest>();

            // turn match case off so all are off
            var caseSwitch = comp.Find(".case-switch input.mud-switch-input");
            caseSwitch.Change(false);

            comp.Render();
            // should not have any remnants of a brace being open
            comp.WaitForAssertion(() => comp.FindAll(".brace-open").Count.Should().Be(0));
            comp.FindAll("code.match-braces").Count.Should().Be(0);
            comp.Find("style.invisible-hide").Should().NotBeNull();
            comp.FindAll("pre.line-numbers").Count.Should().Be(0);

            // turn all options on
            caseSwitch = comp.Find(".case-switch input.mud-switch-input");
            caseSwitch.Change(true);
            var invSwitch = comp.Find(".invisible-switch input.mud-switch-input");
            invSwitch.Change(true);
            var lineSwitch = comp.Find(".line-switch input.mud-switch-input");
            lineSwitch.Change(true);
            comp.Render();
            comp.WaitForAssertion(() => comp.FindAll("style.invisible-hide").Count.Should().Be(0));
            comp.FindAll("code.match-braces").Count.Should().Be(1);
            comp.FindAll("pre.line-numbers").Count.Should().Be(1);
        }

        [TestCase(Origin.TopLeft, Placement.Right)]
        [TestCase(Origin.CenterLeft, Placement.Right)]
        [TestCase(Origin.BottomLeft, Placement.Right)]
        [TestCase(Origin.TopRight, Placement.Left)]
        [TestCase(Origin.CenterRight, Placement.Left)]
        [TestCase(Origin.BottomRight, Placement.Left)]
        [TestCase(Origin.BottomCenter, Placement.Top)]
        [TestCase(Origin.TopCenter, Placement.Bottom)]
        public void GetPlacement_ShouldReturnExpectedPlacement(Origin origin, Placement expected)
        {
            var method = typeof(MudXCodeBlock).GetMethod("GetPlacement", BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull();
            var resultObj = method!.Invoke(null, [origin]);
            resultObj.Should().NotBeNull();
            var result = (Placement)resultObj!;
            result.Should().Be(expected);
        }

        [Test]
        public async Task DisposeAsync_ShouldDisposeResources()
        {
            var codeFiles = new[] { new CodeFile("Test", "code", CodeLanguage.CSharp) };
            var comp = Context.RenderComponent<MudXCodeBlock>(p => p.Add(x => x.Codes, codeFiles));
            var disposeTask = comp.Instance.DisposeAsync();
            await disposeTask;
            // No exception means success; further resource checks would require more setup/mocking
        }
    }
}