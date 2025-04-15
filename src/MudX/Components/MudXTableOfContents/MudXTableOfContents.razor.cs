using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.State;
using MudBlazor.Utilities;

namespace MudX
{
    public partial class MudXTableOfContents : MudComponentBase, IAsyncDisposable
    {
        private bool _contentDrawerOpen = true;
        private readonly string _tocId = Guid.NewGuid().ToString();

        private readonly ParameterState<bool> _contentDrawerOpenState;

        private List<MudXTableOfContentsSection> _sections = [];
        private IScrollSpy? _scrollSpy;

        public MudXTableOfContents()
        {
            using var registerScope = CreateRegisterScope();
            _contentDrawerOpenState = registerScope.RegisterParameter<bool>(nameof(ContentDrawerOpen))
                .WithParameter(() => ContentDrawerOpen)
                .WithChangeHandler(HandleContentDrawerOpenChanged);
        }

        protected string GetNavLinkClass(MudXTableOfContentsSection section) =>
            new CssBuilder("mudx-toc-nav-navlink")
                .AddClass("active", section.Active)
                .AddClass($"navigation-level-{section.Level}")
                .Build();

        [Inject]
        private IScrollSpyFactory ScrollSpyFactory { get; set; } = null!;

        /// <summary>
        /// Whether the Table of Contents drawer is open or closed
        /// </summary>
        /// <remarks>Defaults to <see langword="true"/></remarks>
        [Parameter]
        public bool ContentDrawerOpen { get; set; } = true;

        /// <summary>
        /// The text displayed above the section links, this will display at the top of the MudDrawer if there is at least one section.
        /// </summary>
        /// <remarks>Defaults to "Contents"</remarks>
        [Parameter]
        public string Headline { get; set; } = "Contents";

        /// <summary>
        /// Whether the scroll to top button is visible
        /// </summary>
        /// <remarks>Defaults to <see langword="true"/></remarks>
        [Parameter]
        public bool ScrollToTopVisible { get; set; } = true;

        /// <summary>
        /// The content of the Table of Contents Document
        /// </summary>
        [Parameter, EditorRequired]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Where you want the Table of Contents to be rendered
        /// </summary>
        /// <remarks>Defaults to <see cref="Anchor.End"/></remarks>
        [Parameter]
        public Anchor Anchor { get; set; } = Anchor.End;

        /// <summary>
        /// The Elevation of the Table of Contents Mud Drawer
        /// </summary>
        /// <remarks>Defaults to <see langword="0"/></remarks>
        [Parameter]
        public int Elevation { get; set; }

        /// <summary>
        /// The Breakpoint at which the Table of Contents should be hidden
        /// </summary>
        /// <remarks>Defaults to <see cref="Breakpoint.Lg"/></remarks>
        [Parameter]
        public Breakpoint Breakpoint { get; set; } = Breakpoint.Lg;

        /// <summary>
        /// The Clip Mode for the Table of Contents
        /// </summary>
        /// <remarks>Defaults to <see cref="DrawerClipMode.Always"/></remarks>
        [Parameter]
        public DrawerClipMode DrawerClipMode { get; set; } = DrawerClipMode.Always;

        /// <summary>
        /// The Header for the Table of Contents Items
        /// </summary>
        [Parameter]
        public RenderFragment? HeaderContent { get; set; }

        /// <summary>
        /// The Footer for the Table of Contents Items
        /// </summary>
        [Parameter]
        public RenderFragment? FooterContent { get; set; }

        /// <summary>
        /// The CSS selector used to identify the scroll container
        /// </summary>
        [Parameter]
        public string ScrollContainerSelector { get; set; } = "html";

        /// <summary>
        /// The class name (without .) to identify the HTML elements that should be observed for viewport changes
        /// </summary>
        [Parameter]
        public string SectionClassSelector { get; set; } = "mudx-table-of-contents";

        // If the user toggles the content drawer, update the drawer open variable
        private void HandleContentDrawerOpenChanged(ParameterChangedEventArgs<bool> args)
        {
            _contentDrawerOpen = args.Value;
        }

        // Setup the scrollspy
        protected override void OnInitialized()
        {
            _scrollSpy = ScrollSpyFactory.Create();
        }

        // After IJsRuntime is available, start the scrollspy on elments with the specified classes
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (_scrollSpy is not null)
                {
                    _scrollSpy.ScrollSectionSectionCentered += ScrollSpy_ScrollSectionSectionCentered;

                    if (!string.IsNullOrEmpty(SectionClassSelector))
                    {
                        await _scrollSpy.StartSpying(ScrollContainerSelector, SectionClassSelector);
                    }

                    SelectActiveSection(_scrollSpy.CenteredSection);
                }
            }
        }

        /// <summary>
        /// Adds a section to the Table of Contents
        /// </summary>
        public async Task AddSectionAsync(MudXTableOfContentsSection section)
        {
            // Add section logic
            _sections.Add(section);
            await Task.CompletedTask;
        }

        private Task OnNavLinkClick(string id)
        {
            return _scrollSpy is not null
                ? _scrollSpy.ScrollToSection(id)
                : Task.CompletedTask;
        }

        private void ScrollSpy_ScrollSectionSectionCentered(object? sender, ScrollSectionCenteredEventArgs e) =>
             SelectActiveSection(e.Id);

        private void SelectActiveSection(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            var activeLink = _sections.FirstOrDefault(x => x.Id == id);
            if (activeLink == null)
            {
                return;
            }

            _sections.ToList().ForEach(item => item.Deactive());
            activeLink.Activate();

            StateHasChanged();
        }

        /// <summary>
        /// Scrolls to a section based on the fragment of the uri. If there is no fragment, no scroll will occurr
        /// </summary>
        /// <param name="uri">The uri containing the fragment to scroll</param>
        /// <returns>A task that completes when the viewport has scrolled</returns>
        public Task ScrollToSection(Uri uri)
        {
            return _scrollSpy is not null
                ? _scrollSpy.ScrollToSection(uri)
                : Task.CompletedTask;
        }


        // Dispose the scrollspy
        public async ValueTask DisposeAsync()
        {
            if (_scrollSpy is null)
            {
                return;
            }

            _scrollSpy.ScrollSectionSectionCentered -= ScrollSpy_ScrollSectionSectionCentered;
            if (IsJSRuntimeAvailable)
            {
                await _scrollSpy.DisposeAsync();
            }
        }
    }
}
