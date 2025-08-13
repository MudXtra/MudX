using System.Collections.Generic;
using MudX;

namespace MudX.Docs.Examples
{
    public static class BlazorLottieActionsExampleCode
    {
        public static readonly IEnumerable<CodeFile> Files = new[]
        {
            new CodeFile
            (
                Title: "BlazorLottieActionsExample.razor",
                Code: @"@namespace MudX.Docs.LottiePlayer
@using System.Globalization

<div class=""d-flex mx-auto"" style=""width: 175px;"">
    <LottiePlayer @ref=""@_lottiePlayer""
                  Class=""my-4""
                  CurrentFrameChanged=""@CurrentFrameChangedEvent""
                  Src=""@_lottieSrc"" />
</div>

<MudGrid Class=""d-flex"">
    <MudItem xs=""4"" md=""3"" Class=""d-flex align-center justify-center"">
        <MudButton Variant=""Variant.Filled"" Color=""Color.Primary"" OnClick=""@ToggleAnimation"">@(_play ? ""Pause"" : ""Play"")</MudButton>
    </MudItem>
    <MudItem xs=""4"" md=""3"" Class=""d-flex justify-center align-items"">
        <MudSlider @bind-Value=""@_speed"" Min=""0"" Max=""3"" Step="".1"" ValueLabel
                   @bind-Value:after=""@(async () => await _lottiePlayer!.SetSpeedAsync(_speed))"">
            Speed: @_speed.ToString(""0.0"", CultureInfo.CurrentCulture)
        </MudSlider>
    </MudItem>
    <MudItem xs=""4"" md=""3"" Class=""d-flex justify-center align-center"">
        <MudSwitch T=""bool"" Value=""_forward"" ValueChanged=""@ChangeDirectionValue""
                   LabelPlacement=""Placement.Left"" Color=""Color.Primary"">Reverse</MudSwitch>
        <MudText Class=""mud-switch mud-switch-label-medium mud-input-content-placement-end"">Forward</MudText>
    </MudItem>
    <MudItem xs=""4"" md=""3"" Class=""d-flex justify-center align-items-center"">
        <MudText>
            Total Frames: @_lottiePlayer!.TotalAnimationFrames.ToString(""0.0"", CultureInfo.CurrentCulture)<br/>
            Current Frame: @_lottiePlayer!.CurrentAnimationFrame.ToString(""0.0"", CultureInfo.CurrentCulture)
        </MudText>
    </MudItem>
</MudGrid>

@code {
    private LottiePlayer _lottiePlayer = default!;
    private string _lottieSrc = ""./_content/MudX.Docs/lottie/loading.json"";

    private bool _play = true;
    private double _speed = 1.0;
    private bool _forward = true;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        // You can set the source dynamically if needed
        // _lottieSrc = ""https://path.to/your/lottie.json"";
    }

    private async Task ChangeDirectionValue(bool val)
    {
        if (val)
        {
            await _lottiePlayer!.SetDirectionAsync(LottieAnimationDirection.Forward);
        }
        else
        {
            await _lottiePlayer!.SetDirectionAsync(LottieAnimationDirection.Reverse);
        }
        _forward = val;
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

    private void CurrentFrameChangedEvent(LottiePlayerEventFrameArgs args)
    {
        // need event to report on frames
        if (args.CurrentTime > 999999)
        {
            _play = !_play;
        }
    }
}",
                Language: CodeLanguage.Razor
            )
        };
    }
}
