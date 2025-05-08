namespace MudX.DependsOn.Data.Models
{
    public record DependencyVersion(string Name, string Version, string Url)
    {
        public override string ToString() => $"{Name} - Version: {Version}";
    }
}
