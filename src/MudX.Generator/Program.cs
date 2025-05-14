using MudX.Generator.JSCompiler;

// this will compress and put all module js files into wwwroot
if (args.Length < 2)
{
    Console.WriteLine();
    Console.WriteLine("MudX.Generator: 2 arguments required, but only " + args.Length + " provided.");
    Console.WriteLine("MudX.Generator: 1st argument is the output folder, e.g. wwwroot");
    Console.WriteLine("MudX.Generator: 2nd argument is the base directory to scan including subdirectories and where the new js files will go. e.g. ../MudX/Scripts");
    Console.WriteLine();
    Console.WriteLine("******");
    return 1;
}
try
{
    var outputFolder = args[0];
    var jsFiles = args[1];
    var outputDirectory = Path.GetFullPath(Path.Combine("..", "MudX", outputFolder));
    if (!Directory.Exists(outputDirectory))
    {
        throw new Exception($"Output directory does not exist: {outputDirectory}");
    }
    // see if subdirectory of outputDirectory exists called modules, create it if it doesn't exist and set it to modulesDirectory
    var modulesDirectory = Path.Combine(outputDirectory, "modules");
    if (!Directory.Exists(modulesDirectory))
    {
        Directory.CreateDirectory(modulesDirectory);
    }
    var jsDirectory = Path.GetFullPath(Path.Combine("..", "MudX", jsFiles));
    if (!Directory.Exists(jsDirectory))
    {
        throw new Exception($"Base directory does not exist: {jsDirectory}");
    }
    // get a List<filenames> of all js files in jsDirectory including subdirectories
    var jsFilesList = Directory.GetFiles(jsDirectory, "*.js", SearchOption.AllDirectories).ToList();

    foreach (var jsFile in jsFilesList)
    {
        var jsFileName = Path.GetFileName(jsFile);
        var jsOutFile = Path.Combine(modulesDirectory, jsFileName);
        var contents = JavaScriptCompressor.Compress(File.ReadAllText(jsFile));
        // write contents to file
        File.WriteAllText(jsOutFile, contents);
    }
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("****** ERROR ******");
    Console.WriteLine("MudX.Generator: " + ex.Message);
    Console.WriteLine();
    Console.WriteLine("******");
    return 1;
}

