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
        internal List<DependencyVersion> Versions { get; set; } = [];
    }
}
