namespace MudX.Docs.Data
{
    public class VersionsList
    {
        public VersionsList()
        {
            Versions =
                [
                    new DependencyVersion("MudBlazor", typeof(MudBlazor.MudAlert).Assembly.GetName().Version!.ToString(), "https://mudblazor.com/"),
                    new DependencyVersion("Blazor.Lottie.Player", typeof(Blazor.Lottie.Player.LottiePlayer).Assembly.GetName().Version!.ToString(), "https://github.com/MudXtra/LottiePlayer"),                
                ];
        }
        internal List<DependencyVersion> Versions { get; set; } = [];
    }
}
