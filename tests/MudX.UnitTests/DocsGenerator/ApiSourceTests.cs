using System.Reflection;
using AwesomeAssertions;

namespace MudX.UnitTests.DocsGenerator;

public class ApiSourceTests
{
    [Fact]
    public void FindPackageXmlPath_SelectsRequestedVersionAndFramework()
    {
        var packageRoot = Directory.CreateTempSubdirectory();
        var expectedPath = Path.Combine(packageRoot.FullName, "9.9.0", "lib", "net10.0", "MudBlazor.xml");
        var stalePath = Path.Combine(packageRoot.FullName, "8.0.0", "lib", "net10.0", "MudBlazor.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(stalePath)!);
        File.WriteAllText(expectedPath, "9.9.0");
        File.WriteAllText(stalePath, "8.0.0");

        try
        {
            var apiSourceType = Assembly.LoadFrom(GetDocsGeneratorAssemblyPath())
                .GetType("MudX.Docs.Generator.ApiSource", throwOnError: true)!;
            var findPackageXmlPath = apiSourceType.GetMethod(
                "FindPackageXmlPath",
                BindingFlags.NonPublic | BindingFlags.Static);

            findPackageXmlPath.Should().NotBeNull();
            var selectedPath = (string?)findPackageXmlPath!.Invoke(
                null,
                [packageRoot.FullName, "9.9.0", "net10.0", "MudBlazor.xml"]);

            selectedPath.Should().Be(expectedPath);
        }
        finally
        {
            packageRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public void LoadXmlDocumentation_PreservesReadableTextForSeeCrefElements()
    {
        var xmlPath = Path.GetTempFileName();
        File.WriteAllText(xmlPath, """
            <doc>
              <members>
                <member name="P:Example.Component.Separator">
                  <summary>The separator shown between items.</summary>
                  <remarks>Defaults to /. Will not be shown if <see cref="P:Example.Component.SeparatorTemplate" /> is set. Uses <see cref="T:System.String">custom text</see>.</remarks>
                </member>
              </members>
            </doc>
            """);

        try
        {
            var apiSourceType = Assembly.LoadFrom(GetDocsGeneratorAssemblyPath())
                .GetType("MudX.Docs.Generator.ApiSource", throwOnError: true)!;
            var loadXmlDocumentation = apiSourceType.GetMethod(
                "LoadXmlDocumentation",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            var docs = (Dictionary<string, string>)loadXmlDocumentation.Invoke(null, [xmlPath])!;

            docs["P:Example.Component.Separator"].Should().Be(
                "The separator shown between items.\nRemarks: Defaults to /. Will not be shown if SeparatorTemplate is set. Uses custom text.");
        }
        finally
        {
            File.Delete(xmlPath);
        }
    }

    private static string GetDocsGeneratorAssemblyPath()
    {
        var repositoryRoot = FindRepositoryRoot();
        var testConfiguration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";

        return Path.Combine(
            repositoryRoot.FullName,
            "src",
            "MudX.Docs.Generator",
            "bin",
            testConfiguration,
            "net10.0",
            "MudX.Docs.Generator.dll");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                    directory.FullName,
                    "src",
                    "MudX.Docs.Generator",
                    "MudX.Docs.Generator.csproj")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the MudX repository root.");
    }
}
