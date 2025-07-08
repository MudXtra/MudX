using Bunit;
using FluentAssertions;
using MudBlazor;
using MudX.UnitTests.Viewer.TestComponents.Outline;
using NUnit.Framework;

namespace MudX.UnitTests.Components
{
    [TestFixture]
    public class OutlineTests : BunitTest
    {
        [Test]
        public void Outline_ShouldRender()
        {
            var provider = Context.RenderComponent<MudPopoverProvider>();
            var comp = Context.RenderComponent<OutlineBasicTest>();
            // the entire outline
            var divs = comp.FindAll(".mudx-toc-document");
            divs.Count.Should().Be(1);
            // the outline context
            divs = comp.FindAll(".mudx-toc-content");
            divs.Count.Should().Be(1);
            // the table of contents
            divs = provider.FindAll(".mudx-outline-popover");
            divs.Count.Should().Be(1);
            // the sections
            divs = comp.FindAll(".mudx-toc-section");
            divs.Count.Should().Be(3);
        }

        [Test]
        public void Outline_NestedSections_ShouldRenderCorrectly()
        {
            var provider = Context.RenderComponent<MudPopoverProvider>();
            var comp = Context.RenderComponent<OutlineNestingTest>();
            // Top-level sections: Item A, Item B
            var topSections = provider.FindAll(".mudx-toc-nav-navlink");
            var sections = comp.FindComponents<MudXOutlineSection>();
            // There are 7 sections in total (including nested)
            topSections.Count.Should().Be(7);
            sections.Count.Should().Be(7);
            var lastSection = sections[6];
            var sectionBefore = sections[5];

            // Check that nested section titles exist
            comp.Markup.Should().Contain("Item 1a");
            comp.Markup.Should().Contain("Item 3");

            // Level for lastSection should be 3, render does -1
            lastSection.Instance.Level.Should().Be(3);
            topSections[6].ClassList.Contains("navigation-level-2");
            sectionBefore.Instance.Level.Should().Be(2);
            topSections[5].ClassList.Contains("navigation-level-1");
        }

        [Test]
        public void Outline_SectionTitlesAndContent_ShouldRender()
        {
            var comp = Context.RenderComponent<OutlineBasicTest>();
            comp.Markup.Should().Contain("Item 1");
            comp.Markup.Should().Contain("Item 2");
            comp.Markup.Should().Contain("Item 3");
            comp.Markup.Should().Contain("Lorem ipsum");
        }

        [Test]
        public void Outline_SectionIds_ShouldBeUniqueAndValid()
        {
            var comp = Context.RenderComponent<OutlineNestingTest>();
            var sections = comp.FindAll(".mudx-toc-section");
            var ids = sections.Select(s => s.GetAttribute("id")).ToList();

            // All ids should be non-null, non-empty, and unique
            ids.Should().OnlyHaveUniqueItems();
            ids.Should().OnlyContain(id => !string.IsNullOrWhiteSpace(id));
        }

        [Test]
        public void Outline_ActiveSection_OnlyOneActiveAtATime()
        {
            var provider = Context.RenderComponent<MudPopoverProvider>();
            var comp = Context.RenderComponent<OutlineBasicTest>();
            // Simulate clicking the first nav link
            var navLinks = provider.FindAll(".mudx-toc-nav-navlink .mud-nav-link");
            navLinks[0].Click();

            // Only one nav link should have the 'active' class
            navLinks = provider.FindAll(".mudx-toc-nav-navlink");
            navLinks.Count(l => l.ClassList.Contains("active")).Should().Be(1);
        }

        [Test]
        public async Task Outline_GetId_Ensure_Unique()
        {
            var comp = Context.RenderComponent<OutlineBasicTest>();
            var outline = comp.FindComponent<MudXOutline>();
            var sections = comp.FindComponents<MudXOutlineSection>();
            sections.Count.Should().Be(3);
            sections[2].Instance.Title.Should().Be("Item 3");
            var newSection = Context.RenderComponent<MudXOutlineSection>(p => p
                .Add(p => p.Title, "Item 3")); // create an outline section with a duplicate title
            newSection.Instance.ParentContainer = outline.Instance;
            await outline.Instance.RegisterSectionAsync(newSection.Instance);
            outline.Instance.RegisterUniqueIds(newSection.Instance);
            var newId = newSection.Instance.SectionId;
            newId.Should().Be("item-3-1");

            // create a section with a title with odd characters
            var oddSection = Context.RenderComponent<MudXOutlineSection>(p => p
                .Add(p => p.Title, "#!$ _ 77"));
            oddSection.Instance.ParentContainer = outline.Instance;
            await outline.Instance.RegisterSectionAsync(oddSection.Instance);
            outline.Instance.RegisterUniqueIds(oddSection.Instance);
            newId = oddSection.Instance.SectionId;
            // SectionId must start with an alpha character and regex removes all non alpha numeric (incl spaces)
            // and replaces them with dashes. If it starts with a non alpha character it preprends the front
            // with section- so section- + --- + _ + - + 77
            newId.Should().Be("section-----_-77");
        }

        [Test]
        public async Task Outline_ContentDrawer_ByBreakpoint()
        {
            var comp = Context.RenderComponent<OutlineBasicTest>();
            var outline = comp.FindComponent<MudXOutline>();
            outline.Should().NotBeNull();
            outline.Instance.TOCBreakpoint.Should().Be(Breakpoint.Md);
            await comp.InvokeAsync(async () => await outline.Instance.PositionChanged(this, Breakpoint.Lg));
            comp.WaitForAssertion(() => outline.Instance._contentDrawerOpenState.Value.Should().BeTrue());
            await comp.InvokeAsync(async () => await outline.Instance.PositionChanged(this, Breakpoint.Md));
            comp.WaitForAssertion(() => outline.Instance._contentDrawerOpenState.Value.Should().BeFalse());
        }

        [Test]
        public async Task Outline_Tests_JSModule()
        {
            // Arrange: Setup JSInterop to expect the import and initialize calls
            var jsInterop = Context.JSInterop;

            // Setup the import call to return a mock module
            var moduleMock = jsInterop.SetupModule("./_content/MudX/modules/mudxScrollSpy.js");
            // Setup the initialize call to return true
            moduleMock.Setup<bool>("createScrollSpy", _ => true);
            moduleMock.Setup<bool>("spying", _ => true);
            moduleMock.Setup<bool>("activateSection", _ => true);
            moduleMock.Setup<bool>("scrollToSection", _ => true);
            moduleMock.Setup<bool>("disposeScrollSpy", _ => true);

            var comp = Context.RenderComponent<OutlineBasicTest>();
            var outline = comp.FindComponent<MudXOutline>();
            outline.Should().NotBeNull();
            var sections = comp.FindComponents<MudXOutlineSection>();
            sections.Count.Should().Be(3);
            // Assert: Verify the JS module was imported
            jsInterop.VerifyInvoke("import")
                .Arguments[0].Should().Be("./_content/MudX/modules/mudxScrollSpy.js");

            // Assert: Verify the initialize method was called
            moduleMock.VerifyInvoke("createScrollSpy");
            // Assert: Verify the spying method was called
            moduleMock.VerifyInvoke("spying");
            // Assert: Verify the first section is the active section
            sections[0].Instance.Active.Should().BeTrue();
            moduleMock.VerifyInvoke("activateSection");
            // verify scrollToSection & disposeScrollSpy has not been called
            moduleMock.VerifyNotInvoke("scrollToSection");
            moduleMock.VerifyNotInvoke("disposeScrollSpy");
            await outline.Instance.OnNavLinkClick(sections[1].Instance);
            moduleMock.VerifyInvoke("scrollToSection");
            // dispose the component
            await outline.Instance.DisposeAsync();
            comp.WaitForAssertion(() => moduleMock.VerifyInvoke("disposeScrollSpy"));
        }
    }
}
