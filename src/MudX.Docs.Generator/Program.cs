
using System.Text.Json;
using MudX.Docs.Generator;
using MudX.Docs.NavGenerator;

if (args.Length < 2)
{
    Console.WriteLine("******");
    Console.WriteLine();
    Console.WriteLine("MudX.Docs.Generator: 2 arguments required, but only " + args.Length + " provided.");
    Console.WriteLine("MudX.Docs.Generator: 1st argument is the output file name, e.g. nav-structure.json");
    Console.WriteLine("MudX.Docs.Generator: 2nd argument is the base directory to scan and where the new file will go. e.g. ../MudX.Docs/Components/Docs");
    Console.WriteLine();
    Console.WriteLine("******");
    return 1;
}
try
{
    Console.WriteLine("******");
    Console.WriteLine();
    Console.WriteLine("MudX.Docs.Generator: Building Nav Structure");
    var outputFileName = args[0];
    var rootDirectory = args[1];
    var outputDirectory = Path.GetDirectoryName(rootDirectory);
    if (!Directory.Exists(outputDirectory))
    {
        throw new Exception("Output directory does not exist {" + outputDirectory + "}");
    }
    var navItems = GenerateNav.GenerateNavFromRazorFiles(rootDirectory);

    var json = JsonSerializer.Serialize(navItems, new JsonSerializerOptions
    {
        WriteIndented = true
    });

    File.WriteAllText(outputFileName, json);
    Console.WriteLine("MudX.Docs.Generator: Nav structure written to {" + outputDirectory + "/" + outputFileName + "}");
    Console.WriteLine();
    var result = ExampleSource.GenerateExamples(rootDirectory);
    if (!result)
    {
        return 1;
    }
    Console.WriteLine("MudX.Docs.Generator: Examples written to {" + outputDirectory + "/Examples}");
    Console.WriteLine();
    var apiResult = ApiSource.GenerateApiDocs(rootDirectory);
    if (!apiResult)
    {
        return 1;
    }
    Console.WriteLine();
    Console.WriteLine("******");
    return 0;

}
catch (Exception ex)
{
    Console.WriteLine("******");
    Console.WriteLine();
    Console.WriteLine("****** ERROR ******");
    Console.WriteLine("MudX.Docs.Generator: " + ex.Message);
    Console.WriteLine();
    Console.WriteLine("******");
    return 1;
}

