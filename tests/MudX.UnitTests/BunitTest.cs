using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NUnit.Framework;

namespace MudX.UnitTests
{
    public abstract class BunitTest
    {
        protected Bunit.TestContext Context { get; private set; } = null!;

        [SetUp]
        public virtual void Setup()
        {
            Context = new();
            Context.AddTestServices();
        }

        [TearDown]
        public void TearDown() => Context.Dispose();
    }

    public static class TestContextExtensions
    {
        public static void AddTestServices(this Bunit.TestContext ctx)
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
