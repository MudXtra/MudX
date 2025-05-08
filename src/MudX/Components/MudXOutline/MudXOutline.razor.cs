using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.State;
using MudBlazor.Utilities;
using MudX.Components.MudXOutline;
using MudX.Extensions;

namespace MudX
{
    public partial class MudXOutline : MudComponentBase, IAsyncDisposable, IOutlineContainer
    {
        private readonly string _tocId = Guid.NewGuid().ToString();

        private readonly ParameterState<bool> _contentDrawerOpenState;

        internal List<MudXOutlineSection> _sections = [];
        private IScrollSpy? _scrollSpy;

        public MudXOutline()
        {
            using var registerScope = CreateRegisterScope();
            _contentDrawerOpenState = registerScope.RegisterParameter<bool>(nameof(ContentDrawerOpen))
                .WithParameter(() => ContentDrawerOpen)
                .WithChangeHandler(HandleContentDrawerOpenChanged);
        }

        protected static string GetNavLinkClass(MudXOutlineSection section) =>
            new CssBuilder("mudx-toc-nav-navlink")
                .AddClass("active", section.Active)
                .AddClass($"navigation-level-{Math.Min(5, section.Level - 1)}", section.Level > 1)
                .Build();

        protected string GetNavLinksClass => new CssBuilder("mudx-toc-nav-links")
            .AddClass($"mudx-style-{StyleVariant.ToDescription()}", StyleVariant != OutlineStyleVariant.None)
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
        /// The width of the Table of Contents drawer in pixels.
        /// </summary>
        /// <remarks>Defaults to 200. Values such as 200 or 300 become 200px or 300px respectively.</remarks>
        [Parameter]
        public int Width { get; set; } = 200;

        /// <summary>
        /// The color of the Table of Contents drawer
        /// </summary>
        /// <remarks>Defaults to <see cref="Color.Transparent"/></remarks>
        [Parameter]
        public Color Color { get; set; } = Color.Transparent;

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
        public DrawerClipMode DrawerClipMode { get; set; } = DrawerClipMode.Never;

        /// <summary>
        /// The CSS selector used to identify the scroll container
        /// </summary>
        [Parameter]
        public string ScrollContainerSelector { get; set; } = "html";

        /// <summary>
        /// The class name (without .) to identify the HTML elements that should be observed for viewport changes
        /// </summary>
        [Parameter]
        public string SectionClassSelector { get; set; } = "mudx-toc-section";

        /// <summary>
        /// The Style Variant to apply to the Table of Contents area. Options are Bullet, Scroll, Minimal, and None.
        /// </summary>
        /// <remarks>Defaults to <see cref="OutlineStyleVariant.Scroll"/></remarks>
        [Parameter]
        public OutlineStyleVariant StyleVariant { get; set; } = OutlineStyleVariant.Scroll;

        public int Level { get; } = 0;

        // If the user toggles the content drawer, update the drawer open variable
        private void HandleContentDrawerOpenChanged(ParameterChangedEventArgs<bool> args)
        {
            StateHasChanged();
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
                // make sure each level is put in the correct order regardless if nested
                BuildLevelStructure();
                // make sure each section has a unique SectionId for ScrollSpy 
                BuildSectionIdsUnique();
                if (_scrollSpy is not null)
                {
                    _scrollSpy.ScrollSectionSectionCentered += ScrollSpy_ScrollSectionSectionCentered;

                    if (!string.IsNullOrEmpty(SectionClassSelector))
                    {
                        await _scrollSpy.StartSpying(ScrollContainerSelector, SectionClassSelector);
                    }

                    var section = _sections.FirstOrDefault();
                    if (section is { SectionId: not null })
                        SelectActiveSection(section.SectionId);
                }
            }
        }

        private void BuildLevelStructure()
        {
            // Set the Level structure so hierarchial sections can be rendered correctly in the Table of Contents
            foreach (var section in _sections)
            {
                section.SetLevelStructure();
            }
        }

        /// <summary>
        /// Ensure each section has a unique SectionId both for ScrollSpy and @key declarations
        /// </summary>
        internal void BuildSectionIdsUnique()
        {
            var idCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            BuildSectionIdsUniqueInternal(_sections, idCounts);
        }

        private void BuildSectionIdsUniqueInternal(List<MudXOutlineSection> sections, Dictionary<string, int> idCounts)
        {
            foreach (var section in sections)
            {
                var baseId = section.SectionId;
                if (!idCounts.TryGetValue(baseId, out int _))
                {
                    idCounts[baseId] = 1;
                    section.SectionId = baseId;
                }
                else
                {
                    idCounts[baseId]++;
                    section.SectionId = $"{baseId}-{idCounts[baseId]}";
                    // Queue up a state change
                    StateHasChanged();
                }

                if (section._subSections.Count > 0)
                {
                    BuildSectionIdsUniqueInternal(section._subSections, idCounts);
                }
            }
        }

        /// <summary>
        /// Adds a section to the Table of Contents
        /// </summary>
        public async Task RegisterSectionAsync(MudXOutlineSection section)
        {
            // Add section logic
            _sections.Add(section);
            await Task.CompletedTask;
        }

        private async Task OnNavLinkClick(string id)
        {
            if (_scrollSpy is not null)
            {
                // stop the event from changing the active section in case it doesn't "center" the section
                _scrollSpy.ScrollSectionSectionCentered -= ScrollSpy_ScrollSectionSectionCentered;
                await _scrollSpy.ScrollToSection(id);
                _scrollSpy.ScrollSectionSectionCentered += ScrollSpy_ScrollSectionSectionCentered;
            }
            SelectActiveSection(id);
        }

        private void ScrollSpy_ScrollSectionSectionCentered(object? sender, ScrollSectionCenteredEventArgs e) =>
             SelectActiveSection(e.Id);

        private void SelectActiveSection(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            var activeLink = _sections.FirstOrDefault(x => x.SectionId == id);
            if (activeLink == null)
            {
                return;
            }

            _sections.ForEach(item => item.Deactivate());
            activeLink.Activate();

            InvokeAsync(StateHasChanged);
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
