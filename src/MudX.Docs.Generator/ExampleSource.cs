using System.Text;

namespace MudX.Docs.Generator
{
    internal static class ExampleSource
    {
        internal static bool GenerateExamples(string rootDirectory)
        {
            var processedFileCount = 0;
            var skippedFileCount = 0;
            var razorFiles = Directory.GetFiles(rootDirectory, "*Example.razor", SearchOption.AllDirectories);

            try
            {
                // Create Examples directory if it doesn't exist
                var examplesDirectory = Path.Combine(rootDirectory, "Examples");
                if (!Directory.Exists(examplesDirectory))
                {
                    Directory.CreateDirectory(examplesDirectory);
                    Console.WriteLine($"Created Examples directory: {examplesDirectory}");
                }

                foreach (var filePath in razorFiles)
                {
                    try
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        var generatedFilePath = Path.Combine(examplesDirectory, $"{fileName}.g.cs");

                        // Check if we need to regenerate based on timestamps
                        if (ShouldSkipGeneration(filePath, generatedFilePath))
                        {
                            skippedFileCount++;
                            continue;
                        }

                        string fileContent = File.ReadAllText(filePath);
                        string originalFileName = Path.GetFileName(filePath);

                        // Escape the content for C# string literal
                        string escapedContent = EscapeForCSharpString(fileContent);

                        var codeBuilder = new StringBuilder();
                        codeBuilder.AppendLine("using System.Collections.Generic;");
                        codeBuilder.AppendLine("using MudX;");
                        codeBuilder.AppendLine();
                        codeBuilder.AppendLine("namespace MudX.Docs.Examples");
                        codeBuilder.AppendLine("{");
                        codeBuilder.AppendLine($"    public static class {fileName}Code");
                        codeBuilder.AppendLine("    {");
                        codeBuilder.AppendLine($"        public static readonly IEnumerable<CodeFile> Files = new[]");
                        codeBuilder.AppendLine("        {");
                        codeBuilder.AppendLine("            new CodeFile");
                        codeBuilder.AppendLine("            (");
                        codeBuilder.AppendLine($"                Title: \"{originalFileName}\",");
                        codeBuilder.AppendLine($"                Code: @\"{escapedContent}\",");
                        codeBuilder.AppendLine("                Language: CodeLanguage.Razor");
                        codeBuilder.AppendLine("            )");

                        // Check if there's a corresponding .razor.cs file
                        var codeFilePath = filePath + ".cs";
                        if (File.Exists(codeFilePath))
                        {
                            string codeFileContent = File.ReadAllText(codeFilePath);
                            string escapedCodeContent = EscapeForCSharpString(codeFileContent);

                            codeBuilder.AppendLine("            ,");
                            codeBuilder.AppendLine("            new CodeFile");
                            codeBuilder.AppendLine("            (");
                            codeBuilder.AppendLine($"                Title: \"{originalFileName}.cs\",");
                            codeBuilder.AppendLine($"                Code: @\"{escapedCodeContent}\",");
                            codeBuilder.AppendLine("                Language: CodeLanguage.CSharp");
                            codeBuilder.AppendLine("            )");
                        }

                        codeBuilder.AppendLine("        };");
                        codeBuilder.AppendLine("    }");
                        codeBuilder.AppendLine("}");

                        // Write the generated file
                        File.WriteAllText(generatedFilePath, codeBuilder.ToString());
                        processedFileCount++;
                    }
                    catch (Exception fileEx)
                    {
                        Console.WriteLine($"Error: Failed to process {filePath}: {fileEx.Message}");
                        return false; // Fail on any file processing error
                    }
                }

                Console.WriteLine("MudX.Docs.Generator: " + processedFileCount + " files processed.");
                Console.WriteLine("MudX.Docs.Generator: " + skippedFileCount + " files skipped (already up to date).");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("******");
                Console.WriteLine();
                Console.WriteLine("****** ERROR ******");
                Console.WriteLine("MudX.Docs.Generator: " + ex.Message);
                Console.WriteLine();
                Console.WriteLine("******");
                return false;
            }
        }

        private static bool ShouldSkipGeneration(string sourceFilePath, string generatedFilePath)
        {
            if (!File.Exists(generatedFilePath))
                return false;

            var sourceLastWrite = File.GetLastWriteTime(sourceFilePath);
            var generatedLastWrite = File.GetLastWriteTime(generatedFilePath);

            // Also check the .razor.cs file if it exists
            var codeFilePath = sourceFilePath + ".cs";
            if (File.Exists(codeFilePath))
            {
                var codeFileLastWrite = File.GetLastWriteTime(codeFilePath);
                // If either source file is newer than generated file, regenerate
                return sourceLastWrite <= generatedLastWrite && codeFileLastWrite <= generatedLastWrite;
            }

            return sourceLastWrite <= generatedLastWrite;
        }

        private static string EscapeForCSharpString(string input)
        {
            return input.Replace("\"", "\"\"");
        }
    }
}
