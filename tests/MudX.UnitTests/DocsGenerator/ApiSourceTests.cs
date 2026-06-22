using System.Reflection;
using AwesomeAssertions;

namespace MudX.UnitTests.DocsGenerator;

public class ApiSourceTests
{
    [Fact]
    public void LoadXmlDocumentation_PreservesReadableTextForSeeCrefElements()
    {
        var xmlPath = Path.GetTempFileName();
        File.WriteAllText(xmlPath, """
            <?xml version="1.0"?>
            <doc>
              <members>
                <member name="P:Example.Component.Separator">
                  <summary>The separator shown between items.</summary>
                  <remarks>Defaults to /. Will not be shown if <see cref="P:Example.Component.SeparatorTemplate" /> is set.</remarks>
                </member>
              </members>
            </doc>
            """);

        try
        {
            var apiSourceType = Assembly.Load("MudX.Docs.Generator")
                .GetType("MudX.Docs.Generator.ApiSource", throwOnError: true)!;
            var loadXmlDocumentation = apiSourceType.GetMethod(
                "LoadXmlDocumentation",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            var docs = (Dictionary<string, string>)loadXmlDocumentation.Invoke(null, [xmlPath])!;

            docs["P:Example.Component.Separator"].Should().Be(
                "The separator shown between items.\nRemarks: Defaults to /. Will not be shown if SeparatorTemplate is set.");
        }
        finally
        {
            File.Delete(xmlPath);
        }
    }
}
