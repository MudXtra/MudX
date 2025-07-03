namespace MudX.DependsOn.Data.Models
{
    /// <summary>
    /// Represents a dependency with its name, version, and associated URL.
    /// </summary>
    /// <remarks>This record is typically used to describe the version information of a software dependency,
    /// including its name, version string, and an optional URL for more details.</remarks>
    /// <param name="Name"></param>
    /// <param name="Version"></param>
    /// <param name="Url"></param>
    public record DependencyVersion(string Name, string Version, string Url)
    {
        /// <summary>
        /// Returns a string representation of the object, including its name and version.
        /// </summary>
        /// <returns>A string in the format "<c>Name - Version: Version</c>", where <c>Name</c> is the object's name and
        /// <c>Version</c> is its version.</returns>
        public override string ToString() => $"{Name} - Version: {Version}";
    }
}
