using System.Security.Cryptography;

namespace MudX.Generator
{
    internal static class StaticAssetsFileMethods
    {
        public static void WriteIfDifferent(string outFile, string contents)
        {
            try
            {
                if (!File.Exists(outFile))
                {
                    File.WriteAllText(outFile, contents);
                    Console.WriteLine($"MudX.Generator: Created file: {outFile}");
                }
                else if (File.ReadAllText(outFile) != contents)
                {
                    File.WriteAllText(outFile, contents);
                    Console.WriteLine($"MudX.Generator: Updated file: {outFile}");
                }
                else
                {
                    Console.WriteLine($"MudX.Generator: No changes for file: {outFile}");
                }
            }
            catch
            {
                File.WriteAllText(outFile, contents);
                Console.WriteLine($"MudX.Generator: Override file: {outFile}");
            }
        }


        internal static void CopyIfDifferent(string sourcePath, string destinationPath)
        {
            bool shouldCopy = true;

            if (File.Exists(destinationPath))
            {
                using var sourceStream = File.OpenRead(sourcePath);
                using var destinationStream = File.OpenRead(destinationPath);
                using var sha256 = SHA256.Create();

                var sourceHash = sha256.ComputeHash(sourceStream);
                var destinationHash = sha256.ComputeHash(destinationStream);

                shouldCopy = !sourceHash.SequenceEqual(destinationHash);
            }

            if (shouldCopy)
            {
                File.Copy(sourcePath, destinationPath, true);
                Console.WriteLine($"MudX.Generator: Updated: {destinationPath}");
            }
            else
            {
                Console.WriteLine($"MudX.Generator: No changes for file: {destinationPath}");
            }
        }
    }
}
