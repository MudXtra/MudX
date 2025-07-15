using System.Reflection;

namespace MudX.Utilities
{
    internal static class AssemblyInfo
    {
        private static readonly Assembly _assembly = typeof(MudXProvider).Assembly;

        internal static readonly string Version =
            $"{_assembly.GetName().Version?.ToString()}";

        internal static readonly string PackageId =
            _assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attr => attr.Key == "PackageId")
                ?.Value ?? _assembly.GetName().Name!;

        /// <summary>
        /// The module Path for a module with a supplied file name.
        /// </summary>
        /// <param name="moduleFileName">The full file name of a module including extension, e.g. mudxProvider.js</param>
        /// <returns>The full path for the import of the module, e.g. ./_content/MudX.MudBlazor.Extension/modules/mudxProvider.js</returns>
        internal static string ModulePath(string moduleFileName) =>
            $"./_content/{PackageId}/modules/{moduleFileName}";
    }
}
