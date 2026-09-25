using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NUnit.Framework;

namespace MudX.UnitTests
{
    public abstract class BunitTest
    {
        protected Bunit.BunitContext Context { get; private set; } = null!;

        [SetUp]
        public virtual void Setup()
        {
            Context = new();
            Context.AddTestServices();
        }

        [TearDown]
        public async Task TearDown() => await Context.DisposeAsync();
    }

    public static class TestContextExtensions
    {
        public static void AddTestServices(this Bunit.BunitContext ctx)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddMudServices(options =>
            {
                options.SnackbarConfiguration.ShowTransitionDuration = 0;
                options.SnackbarConfiguration.HideTransitionDuration = 0;
                options.PopoverOptions.CheckForPopoverProvider = false;
            });
            ctx.Services.AddScoped(sp => new HttpClient());
            ctx.Services.AddOptions();
        }
    }
}
