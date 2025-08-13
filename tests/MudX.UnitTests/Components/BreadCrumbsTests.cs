using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudX.UnitTests.Viewer.TestComponents.Breadcrumbs;
using NUnit.Framework;

namespace MudX.UnitTests.Components
{
    [TestFixture]
    public class BreadCrumbsTests : BunitTest
    {

        [Test]
        public void BreadCrumbsShouldRenderCorrectly()
        {
            // Arrange: Render the MudX BreadCrumbs component
            var comp = Context.RenderComponent<BreadcrumbsBasicTest>();
            // Act: Find the breadcrumb container
            var breadcrumbContainer = comp.Find(".mudx-breadcrumbs-wrapper");
            // Assert: Verify the breadcrumb container is rendered
            breadcrumbContainer.Should().NotBeNull();
            // Assert: Verify the default text is present
            breadcrumbContainer.TextContent.Should().Contain("Home", "Default breadcrumb text should be 'Home'.");
        }

        [Test]
        public void BreadCrumbsShouldUpdateHomeText()
        {
            var comp = Context.RenderComponent<BreadcrumbsHomeFormatTest>();
            var breadcrumbsContainer = comp.Find(".mudx-breadcrumbs-wrapper");
            breadcrumbsContainer.Should().NotBeNull();
            breadcrumbsContainer.TextContent.Should().Contain("MudXHome", "Updated breadcrumb Home Text should be 'MudXHome'.");
        }

        [Test]
        public void BreadCrumbs_Test_BuildBreadcrumbsMethod()
        {
            // Arrange: Render the MudXBreadcrumbs component directly
            var navMan = Context.Services.GetRequiredService<NavigationManager>() as NavigationManager;
            var comp = Context.RenderComponent<MudXBreadcrumbs>();

            // Act: Simulate navigation to a nested route
            navMan?.NavigateTo("/section/subsection/page");
            comp.Render(); // Force re-render to process navigation

            // Assert: Find the breadcrumb container
            var breadcrumbsContainer = comp.Find(".mudx-breadcrumbs-wrapper");
            breadcrumbsContainer.Should().NotBeNull();

            // Assert: Check that the breadcrumbs reflect the navigation path
            var text = breadcrumbsContainer.TextContent;
            text.Should().Contain("Home");
            text.Should().Contain("Section");
            text.Should().Contain("Subsection");
            text.Should().Contain("Page");

            // Optionally, check the number of breadcrumb items
            var items = comp.Instance.Items;
            items.Should().NotBeNull();
            items!.Count.Should().Be(4); // Home + 3 segments

            // Assert: The last breadcrumb should be active/current
            items![^1].Disabled.Should().BeTrue();
            items![^1].Text.Should().Be("Page");
        }
    }
}
