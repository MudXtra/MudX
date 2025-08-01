using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class BlazorLottieBasicExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "BlazorLottieBasicExample.razor",
                Code: @"@namespace MudX.Docs.LottiePlayer

<LottiePlayer @ref=""@_lottiePlayer""
              Class=""my-4""
              Style=""height: 200px;""
              Src=""@_lottieSrc"" />

<MudGrid>
    <MudItem xs=""4"" Class=""d-flex justify-center"">
        <MudButton Variant=""Variant.Filled"" Color=""Color.Primary"" OnClick=""@(async () => await _lottiePlayer!.PlayAnimationAsync())"">Play</MudButton>
    </MudItem>
    <MudItem xs=""4"" Class=""d-flex justify-center"">
        <MudButton Variant=""Variant.Filled"" Color=""Color.Primary"" OnClick=""@(async () => await _lottiePlayer!.PauseAnimationAsync())"">Pause</MudButton>
    </MudItem>
    <MudItem xs=""4"" Class=""d-flex justify-center"">
        <MudButton Variant=""Variant.Filled"" Color=""Color.Default"" OnClick=""@(async () => await _lottiePlayer!.StopAnimationAsync())"">Stop</MudButton>
    </MudItem>
</MudGrid>

@code {
    private LottiePlayer _lottiePlayer = default!;
    private string _lottieSrc = ""https://lottie.host/382bbcd7-e7b1-4a30-90cd-884a9c7c3bb1/F7I9nPKEr1.json"";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        // You can set the source dynamically if needed
        // _lottieSrc = ""https://path.to/your/lottie.json"";
    }
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
