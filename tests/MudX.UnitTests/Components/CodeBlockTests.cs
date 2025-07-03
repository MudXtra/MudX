using Bunit;
using FluentAssertions;
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
            // Arrange: Setup necessary parameters and render the component
            var codeFiles = new List<CodeFile>
            {
                new CodeFile("Test.cs", "Console.WriteLine(\"Test\");", CodeLanguage.CSharp)
            };
            var comp = Context.RenderComponent<MudXCodeBlock>(parameters => parameters.Add(p => p.Codes, codeFiles));

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
                new CodeFile("Test.cs", "Console.WriteLine(\"Test\");", CodeLanguage.CSharp)
            };

            // Arrange: Setup JSInterop to expect the import and initialize calls
            var jsInterop = Context.JSInterop;

            // Setup the import call to return a mock module
            var moduleMock = jsInterop.SetupModule("./_content/MudX/modules/mudxPrismWrapper.js");
            // Setup the initialize call to return true
            moduleMock.Setup<bool>("initialize", _ => true);

            var comp = Context.RenderComponent<MudXCodeBlock>(parameters => parameters
                .Add(p => p.Codes, codeFiles)
                .Add(p => p.Theme, CodeTheme.Dark));

            comp.Instance.PrismCSSPath.Should().Be("./_content/MudX/prism/prism-dark.css");

            // Assert: Verify the JS module was imported
            jsInterop.VerifyInvoke("import")
                .Arguments[0].Should().Be("./_content/MudX/modules/mudxPrismWrapper.js");

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
    }
}