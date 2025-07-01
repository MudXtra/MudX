using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.State;
using MudBlazor.Utilities;
using MudX.Components.MudXOutline;
using MudX.Extensions;
using MudX.Utilities;

namespace MudX
{
    /// <summary>
    /// Represents a Table of Contents (TOC) component that provides navigation links to sections within a document.
    /// </summary>
    /// <remarks>The <see cref="MudXOutline"/> component allows users to navigate through sections of a
    /// document or page. It supports features such as  customizable styling, scroll spying, and dynamic section
    /// registration. The TOC can be positioned on the left or right side of the viewport  and includes options for
    /// rendering additional content above or below the section links.</remarks>
    public partial class MudXOutline : MudComponentBase, IAsyncDisposable, IOutlineContainer
    {
        private readonly string _id = $"mudx-toc-{Guid.NewGuid()}";
        private (double X, double Y) _position = (0, 0);
        private bool _shouldRepositionPopover = true;
        private string _scrollContainerSelector = "html";
        private ElementReference _anchorRef;
        private MudPopover? _popoverRef;
        private readonly ParameterState<bool> _contentDrawerOpenState;
        private Anchor _anchor = Anchor.End;
        internal List<MudXOutlineSection> _sections = [];
        private OutlineScrollSpy? _scrollSpy;
        internal Dictionary<string, int> _idCounts = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="MudXOutline"/> class.
        /// </summary>
        /// <remarks>This constructor sets up the internal state and registers parameters required for
        /// managing the content drawer's open state.</remarks>
        public MudXOutline()
        {
            using var registerScope = CreateRegisterScope();
            _contentDrawerOpenState = registerScope.RegisterParameter<bool>(nameof(ContentDrawerOpen))
                .WithParameter(() => ContentDrawerOpen)
                .WithChangeHandler(HandleContentDrawerOpenChanged);
        }

        /// <summary>
        /// Generates the CSS class string for a navigation link based on the specified section's state and level.
        /// </summary>
        /// <param name="section">The section for which the navigation link class is being generated.  Must not be <see langword="null"/>.</param>
        /// <returns>A CSS class string that represents the styling of the navigation link,  including active state, underline
        /// styling, and level-based navigation hierarchy.</returns>
        protected string GetNavLinkClass(MudXOutlineSection section) =>
            new CssBuilder("mudx-toc-nav-navlink")
                .AddClass("active", section.Active)
                .AddClass("underline", section.Active && UnderlineActiveSections)
                .AddClass($"navigation-level-{Math.Min(5, section.Level - 1)}", section.Level > 1)
                .Build();

        /// <summary>
        /// Gets the CSS class string for navigation links, including style variants.
        /// </summary>
        protected string GetNavLinksClass => new CssBuilder("mudx-toc-nav-links")
            .AddClass($"mudx-style-{StyleVariant.ToDescription()}", StyleVariant != OutlineStyleVariant.None)
            .Build();

        /// <summary>
        /// Gets the CSS class names for the popover element, including position and fixed state overrides.
        /// </summary>
        protected string PopoverClassname => new CssBuilder("")
            .AddClass("mud-popover-position-override") // not fixed we hard code the position
            .AddClass("fixed", IsFixed)
            .Build();

        /// <summary>
        /// Gets the CSS class name for the card element, including theme-specific styling based on the configured
        /// color.
        /// </summary>
        protected string CardClassname => new CssBuilder("mudx-toc-nav-popovers")
            .AddClass($"mud-theme-{Color.ToDescriptionString()}")
            .Build();

        /// <summary>
        /// Gets the computed CSS style string for the popover element based on its current state and configuration.
        /// </summary>
        /// <remarks>The styles are conditionally applied depending on the state of the popover, such as
        /// whether it is open, fixed, or if JavaScript runtime is available. This property is primarily used to
        /// dynamically generate inline styles for rendering the popover element.</remarks>
        protected string PopoverStyleName => new StyleBuilder()
            .AddStyle("width", $"{Width}px", _contentDrawerOpenState.Value)
            .AddStyle("max-width", $"{Width}px", _contentDrawerOpenState.Value)
            .AddStyle("z-index", ZIndex.ToString(), _contentDrawerOpenState.Value)
            .AddStyle("top", _position.Y.ToPx(), IsFixed && !IsJSRuntimeAvailable) // fixed position MudPopover will handle resize
            .AddStyle("left", _position.X.ToPx(), IsFixed && !IsJSRuntimeAvailable) // fixed position MudPopover will handle resize
            .Build();

        /// <summary>
        /// Gets the computed CSS style string for the navigation drawer based on its current state and configuration.
        /// </summary>
        protected string NavDrawerStyle => new StyleBuilder()
            .AddStyle("margin-right", $"{Width}px", _contentDrawerOpenState.Value && _anchor == Anchor.Right)
            .AddStyle("margin-left", $"{Width}px", _contentDrawerOpenState.Value && _anchor == Anchor.Left)
            .Build();

        /// <summary>
        /// Inline data attributes for positioning the Table of Contents at the coordinate location.
        /// </summary>
        private Dictionary<string, object> PositionAttributes => new()
        {
            { "data-pc-x", _position.X.ToString(CultureInfo.InvariantCulture) },
            { "data-pc-y", _position.Y.ToString(CultureInfo.InvariantCulture) }
        };

        private bool IsFixed => ScrollContainerSelector == "html";

        [Inject]
        private IJSRuntime? Js { get; set; }

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
        /// The z-index of the Table of Contents drawer
        /// </summary>
        /// <remarks>Defaults to 999.</remarks>
        [Parameter]
        public int ZIndex { get; set; } = 999;

        /// <summary>
        /// Whether to underline the active section
        /// </summary>
        /// <remarks>Defaults to <see langword="true"/></remarks>
        [Parameter]
        public bool UnderlineActiveSections { get; set; } = true;

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
        /// Option content above the table of contents
        /// </summary>
        [Parameter]
        public RenderFragment? HeaderIndexContent { get; set; }

        /// <summary>
        /// Option content below the table of contents
        /// </summary>
        [Parameter]
        public RenderFragment? FooterIndexContent { get; set; }

        /// <summary>
        /// Where you want the Table of Contents to be rendered
        /// </summary>
        /// <remarks>Defaults to <see cref="Anchor.Right"/>. Only Valid values are <see cref="Anchor.Left"/> and <see cref="Anchor.Right"/>.</remarks>
        [Parameter]
        public Anchor Anchor { get; set; } = Anchor.Right;

        /// <summary>
        /// The CSS selector used to identify the scroll container, if using an element id use "#" first and ensure the element starts
        /// with a letter. If using an element class use "." first.
        /// </summary>
        /// <remarks>Defaults to "html", if this is empty or null html will be used</remarks>
        [Parameter]
        public string ScrollContainerSelector { get; set; } = "html";

        /// <summary>
        /// The class name to identify the HTML elements that should be observed for viewport changes inside the 
        /// <see cref="ScrollContainerSelector"/>.
        /// </summary>
        /// <remarks>Defaults to "mudx-toc-section"</remarks>
        [Parameter]
        public string SectionClassSelector { get; set; } = "mudx-toc-section";

        /// <summary>
        /// The Style Variant to apply to the Table of Contents area. Options are Bullet, Scroll, Minimal, and None.
        /// </summary>
        /// <remarks>Defaults to <see cref="OutlineStyleVariant.Scroll"/></remarks>
        [Parameter]
        public OutlineStyleVariant StyleVariant { get; set; } = OutlineStyleVariant.Scroll;

        /// <summary>
        /// The sections of the Table of Contents, this includes _subSections
        /// </summary>
        public List<MudXOutlineSection> Sections
        {
            get => _sections;
        }

        private Origin PopoverAnchor => _anchor == Anchor.Left ? Origin.TopLeft : Origin.TopRight;

        private Origin PopoverTransform => _anchor == Anchor.Left ? Origin.TopRight : Origin.TopLeft;

        /// <summary>
        /// Gets the current level of the object.
        /// </summary>
        public int Level { get; } = 0;

        // If the user toggles the content drawer, update the drawer open variable
        private void HandleContentDrawerOpenChanged(ParameterChangedEventArgs<bool> args)
        {
            StateHasChanged();
        }

        /// <summary>
        /// OnParameterSet override
        /// </summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            _anchor = Anchor switch
            {
                Anchor.Top or Anchor.Start => Anchor.Left,
                Anchor.End or Anchor.Bottom => Anchor.Right,
                _ => Anchor,
            };
            if (string.IsNullOrEmpty(ScrollContainerSelector))
            {
                _scrollContainerSelector = "html";
            }
            else
            {
                _scrollContainerSelector = ScrollContainerSelector;
            }
            _shouldRepositionPopover = true;
        }

        /// <summary>
        /// OnAfterRenderAsync override
        /// After IJsRuntime is available, start the scrollspy on elments with the specified classes
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // make sure each level is put in the correct order regardless if nested
                BuildLevelStructure();
                // Set the Level structure so hierarchial sections can be rendered correctly in the Table of Contents
                RegisterUniqueIds(_sections);
                if (Js is null) throw new Exception("JSRuntime is not available");
                _scrollSpy = new OutlineScrollSpy(Js);
                if (_scrollSpy is not null)
                {
                    _scrollSpy.ScrollSpySectionCentered += ScrollSpySectionCentered;

                    if (!string.IsNullOrEmpty(SectionClassSelector))
                    {
                        await _scrollSpy.StartSpying(_scrollContainerSelector, SectionClassSelector);
                    }

                    var section = _sections.FirstOrDefault();
                    if (section is { SectionId: not null })
                        SelectActiveSection(section.SectionId);
                }
                await PositionIndex();
            }
            if (_shouldRepositionPopover)
            {
                await PositionIndex();
            }
        }

        /// <summary>
        /// Causes an immediate repositioning of the Index
        /// </summary>
        public async Task PositionIndex()
        {
            if (IsJSRuntimeAvailable)
            {
                var rect = await _anchorRef.MudGetBoundingClientRectAsync();
                _position = PagePosition.GetPagePositionFromOrigin(rect, PopoverAnchor);
                _shouldRepositionPopover = false;
                StateHasChanged();
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
        /// Adds a section to the Table of Contents
        /// </summary>
        public async Task RegisterSectionAsync(MudXOutlineSection section)
        {
            // Add section logic
            _sections.Add(section);
            await Task.CompletedTask;
        }

        private void RegisterUniqueIds(List<MudXOutlineSection> sections)
        {
            foreach (var section in sections)
            {
                var sectionId = section.GetId();
                if (!_idCounts.TryAdd(sectionId, 0))
                {
                    _idCounts[sectionId] += 1;
                    section.SectionId = $"{sectionId}-{_idCounts[sectionId]}";
                }
                else
                {
                    section.SectionId = sectionId;
                }
                if (section._subSections.Count > 0)
                {
                    RegisterUniqueIds(section._subSections);
                }
            }
        }

        private async Task OnNavLinkClick(string id)
        {
            if (_scrollSpy is not null)
            {
                // stop the event from changing the active section in case it doesn't "center" the section
                _scrollSpy.ScrollSpySectionCentered -= ScrollSpySectionCentered;
                await _scrollSpy.ScrollToSection(id);
                _scrollSpy.ScrollSpySectionCentered += ScrollSpySectionCentered;
            }
            SelectActiveSection(id);
        }

        private void ScrollSpySectionCentered(object? sender, ScrollSectionCenteredEventArgs e) =>
             SelectActiveSection(e.Id);

        private void SelectActiveSection(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            var activeLink = GetSectionById(id, _sections);
            if (activeLink == null)
            {
                return;
            }

            DeactivateSections(_sections);
            activeLink.Activate();

            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Returns the section or subsection with the specified id
        /// </summary>
        /// <param name="id">The Specified id</param>
        /// <param name="sections">The sections to search</param>
        /// <returns></returns>
        public static MudXOutlineSection? GetSectionById(string id, List<MudXOutlineSection> sections)
        {
            var activeLink = sections.FirstOrDefault(x => x.SectionId == id);
            if (activeLink is not null) return activeLink;
            foreach (var section in sections)
            {
                var result = GetSectionById(id, section._subSections);
                if (result is not null) return result;
            }
            return null;
        }

        /// <summary>
        /// Deactivates all sections and subsections
        /// </summary>
        /// <param name="sections">The sections to deactivate</param>
        public static void DeactivateSections(List<MudXOutlineSection> sections)
        {
            foreach (var section in sections)
            {
                section.Deactivate();
                if (section._subSections.Count > 0)
                {
                    DeactivateSections(section._subSections);
                }
            }
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


        /// <summary>
        /// DisposeAsync
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_scrollSpy is not null)
            {
                _scrollSpy.ScrollSpySectionCentered -= ScrollSpySectionCentered;
                await _scrollSpy.DisposeAsync();
                _scrollSpy = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
