using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;
using MudBlazor;
using MudX.Utilities;

namespace MudX.Components.MudXOutline
{
    /// <summary>
    /// Provides functionality to observe and manage scroll-based interactions with sections of a web page.
    /// </summary>
    public sealed class OutlineScrollSpy : IAsyncDisposable
    {
        private readonly string _spyId = Guid.NewGuid().ToString();
        private readonly IJSRuntime _js;
        private IJSObjectReference? _module;
        private IJSObjectReference? _spyInstance;
        private DotNetObjectReference<OutlineScrollSpy>? _dotNetReference;
        internal bool _isDisposing;

        /// <summary>
        /// The id of the currently centered section
        /// </summary>
        public string? CenteredSection { get; private set; }

        /// <summary>
        /// Event raised when a section is centered
        /// </summary>
        public event EventHandler<ScrollSectionCenteredEventArgs>? ScrollSpySectionCentered;

        /// <summary>
        /// Delegate Task return for PositionChanged
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The breakpoint when PositionChanged is called</param>
        /// <returns></returns>
        public delegate Task BreakpointChangedHandler(object? sender, Breakpoint e);

        /// <summary>
        /// Event raised when a position change occurs
        /// </summary>
        public event BreakpointChangedHandler? PositionChanged;

        /// <summary>
        /// Initialize the class by supplying the IJSRuntime
        /// </summary>
        /// <param name="js"></param>
        [DynamicDependency(nameof(SectionChangeOccured))]
        public OutlineScrollSpy(IJSRuntime js)
        {
            _dotNetReference = DotNetObjectReference.Create(this);
            _js = js;
        }

        /// <summary>
        /// Start the scrollspy on the page
        /// </summary>
        /// <param name="containerSelector">The primary container to observe, usually "html" or an id selector 
        /// such as #maincontainer or class selector such as .mudx-main-container</param>
        /// <param name="sectionClassSelector">The class of the sections to observe. Any element with this class will be observed
        /// when moving to the CenteredSection</param>
        public async Task StartSpying(string containerSelector, string sectionClassSelector)
        {
            if (_isDisposing || _js is null) return;
            // load the module
            _module = await _js.InvokeAsync<IJSObjectReference>("import", AssemblyInfo.ModulePath("mudxScrollSpy.js"));
            // create the scrollspy
            _spyInstance = await _module.InvokeAsync<IJSObjectReference>("createScrollSpy", _spyId);
            // start the scrollspy/setup variables
            await _spyInstance.InvokeVoidAsync("spying", containerSelector, sectionClassSelector, _dotNetReference);
        }

        /// <summary>
        /// Raises the ScrollSpySectionCentered event with the id of the centered section. Typically called in javascript.
        /// </summary>
        /// <param name="id">The id of the centered section</param>
        [JSInvokable]
        public void SectionChangeOccured(string id)
        {
            if (_isDisposing || _spyInstance is null) return;
            if (!string.IsNullOrEmpty(id) && id != CenteredSection)
            {
                CenteredSection = id;
                ScrollSpySectionCentered?.Invoke(this, new ScrollSectionCenteredEventArgs(id));
            }
        }

        /// <summary>
        /// Indicates a position change occured that should cause the TOC to change position
        /// </summary>
        [JSInvokable]
        public async Task UpdatePosition(string breakpoint)
        {
            Breakpoint bp = breakpoint switch
            {
                "xs" => Breakpoint.Xs,
                "sm" => Breakpoint.Sm,
                "md" => Breakpoint.Md,
                "lg" => Breakpoint.Lg,
                "xl" => Breakpoint.Xl,
                _ => Breakpoint.Xl // fallback if unknown
            };
            if (PositionChanged != null)
                await PositionChanged.Invoke(this, bp);
        }


        /// <summary>
        /// Scrolls to a section based on the fragment of the uri. If there is no fragment, no scroll will occurr
        /// </summary>
        public async Task ScrollToSection(Uri uri) => await ScrollToSection(uri.Fragment);

        /// <summary>
        /// Scrolls to a section based on id of the section
        /// </summary>
        /// <param name="id"></param>
        public async Task ScrollToSection(string id)
        {
            if (_isDisposing || _spyInstance is null || string.IsNullOrEmpty(id)) return;
            id = id.Trim('#');
            CenteredSection = id;
            await _spyInstance.InvokeVoidAsync("scrollToSection", id);
        }

        /// <summary>
        /// Sets a section as active by appending a # and id to the history (Current Navigation URL). Scrolling does not occur.
        /// </summary>
        /// <param name="id">The id of the section</param>
        public async Task SetSectionAsActive(string id)
        {
            if (_isDisposing || _spyInstance is null || string.IsNullOrEmpty(id)) return;
            id = id.Trim('#');
            CenteredSection = id;
            await _spyInstance.InvokeVoidAsync("activateSection", id);
        }

        /// <summary>
        /// Dispose the scrollspy, module, and listeners
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            _isDisposing = true;

            if (_spyInstance is not null && _module is not null)
            {
                await _module.InvokeVoidAsync("disposeScrollSpy", _spyId);
                await _spyInstance.DisposeAsync();
                _spyInstance = null;
                await _module.DisposeAsync();
                _module = null;
            }
            _dotNetReference?.Dispose();
            _dotNetReference = null;
        }
    }

}
