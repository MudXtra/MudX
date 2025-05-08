using MudX.DependsOn.Data.Models;

namespace MudX.Docs.Data
{
    public class VersionsList
    {
        public VersionsList()
        {
            Versions =
                [
                    new DependencyVersion("MudBlazor", typeof(MudBlazor.MudAlert).Assembly.GetName().Version!.ToString(), "https://mudblazor.com/"),
                ];
        }
        public List<DependencyVersion> Versions { get; set; } = [];
    }
}
