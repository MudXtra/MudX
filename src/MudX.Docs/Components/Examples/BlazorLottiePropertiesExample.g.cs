using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class BlazorLottiePropertiesExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "BlazorLottiePropertiesExample.razor",
                Code: @"@namespace MudX.Docs.LottiePlayer

<div class=""d-flex mx-auto my-4"" style=""width: 195px;"">
    <LottiePlayer @ref=""@_lottiePlayer""
                  AutoPlay=""@_autoPlay""
                  Src=""@_lottieSrc""
                  LoopCount=""@_loopCount""
                  AnimationType=""@_animationType"" />
</div>

<MudGrid Class=""d-flex align-center justify-center"">
    <MudItem xs=""4"" md=""3"" Class=""d-flex justify-center align-items"">
        <MudSwitch Color=""Color.Primary"" @bind-Value=""@_autoPlay"" @bind-Value:after=""@(() => _counter++)"" LabelPlacement=""Placement.Right"">AutoPlay</MudSwitch>
    </MudItem>
    <MudItem xs=""4"" md=""3"" Class=""d-flex justify-center"">
        <MudButton Variant=""Variant.Filled"" Color=""Color.Primary"" OnClick=""@ToggleAnimation"">@(_play ? ""Pause"" : ""Play"")</MudButton>
    </MudItem>
    <MudItem xs=""4"" md=""3"" Class=""d-flex justify-center align-items"">
        <MudSlider @bind-Value=""@_loopCount"" Min=""0"" Max=""25"" ValueLabel>Loop Count: @_loopCount (0 is infinite)</MudSlider>
    </MudItem>
    <MudItem xs=""4"" md=""3"" Class=""d-flex justify-center flex-row align-items"">
        <MudRadioGroup @bind-Value=""@_animationType"">
            <MudRadio Value=""LottieAnimationType.Svg"" Size=""Size.Small"">SVG</MudRadio>
            <MudRadio Value=""LottieAnimationType.Canvas"" Size=""Size.Small"">Canvas</MudRadio>
        </MudRadioGroup>
    </MudItem>
</MudGrid>

@code {
    private LottiePlayer _lottiePlayer = default!;
    private string _lottieSrc = ""./_content/MudX.Docs/lottie/newAnimation.json"";

    private bool _play = true;
    private bool _autoPlay = true;
    private int _loopCount = 0;
    private LottieAnimationType _animationType = LottieAnimationType.Svg;
    private int _counter = 0;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        // You can set the source dynamically if needed
        // _lottieSrc = ""https://path.to/your/lottie.json"";
    }

    private async Task ToggleAnimation()
    {
        if (_play)
        {
            await _lottiePlayer!.PauseAnimationAsync();
        }
        else
        {
            await _lottiePlayer!.PlayAnimationAsync();
        }
        _play = !_play;
    }
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
