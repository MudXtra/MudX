using MudX.Generator.JSCompiler;
using NUglify;

// this will compress and put all module js files into wwwroot
if (args.Length < 2)
{
    Console.WriteLine();
    Console.WriteLine("MudX.Generator: 2 arguments required, but only " + args.Length + " provided.");
    foreach (var arg in args)
    {
        Console.WriteLine("MudX.Generator: Arg| " + arg);
    }
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
    // At the start of the try block in MudX.Generator
    if (Directory.Exists(modulesDirectory))
    {
        Directory.Delete(modulesDirectory, true); // Delete directory and all contents
    }
    Directory.CreateDirectory(modulesDirectory); // Recreate empty directory
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

    var jsMainFile = Directory.GetFiles(Path.Combine("..", jsDirectory), "mudx.js", SearchOption.AllDirectories).FirstOrDefault();
    if (jsMainFile == null)
    {
        Console.WriteLine("MudX.Generator: Primary JS File not found.");
        return 1;
    }

    var jsDevOutFile = Path.Combine(modulesDirectory, "../", "mudx.js");
    var jsMainOutFile = Path.Combine(modulesDirectory, "../", "mudx.min.js");
    var maincontents = JavaScriptCompressor.Compress(File.ReadAllText(jsMainFile));
    // write contents to file
    File.Delete(jsDevOutFile); // Ensure the file is deleted before copying
    File.Copy(jsMainFile, jsDevOutFile);
    File.WriteAllText(jsMainOutFile, maincontents);

    var cssMainFile = Directory.GetFiles(Path.Combine("..", jsDirectory), "mudx.css", SearchOption.AllDirectories).FirstOrDefault();

    if (cssMainFile == null)
    {
        Console.WriteLine("MudX.Generator: Primary CSS File not found.");
        return 1;
    }

    var cssDevOutFile = Path.Combine(modulesDirectory, "../", "mudx.css");
    var cssMainOutFile = Path.Combine(modulesDirectory, "../", "mudx.min.css");
    var csscontents = Uglify.Css(File.ReadAllText(cssMainFile));
    if (csscontents.HasErrors)
    {
        Console.WriteLine("MudX.Generator: Primary CSS File generated errors during minification.");
        return 1;
    }
    // write contents to file
    File.Delete(cssDevOutFile); // Ensure the file is deleted before copying
    File.Copy(cssMainFile, cssDevOutFile);
    File.WriteAllText(cssMainOutFile, csscontents.Code);

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

