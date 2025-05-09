using Microsoft.JSInterop;
using MudBlazor;

namespace MudX.Components.MudXOutline
{
    public sealed class OutlineScrollSpy : IScrollSpy, IAsyncDisposable
    {
        private readonly IJSRuntime _js;
        private IJSObjectReference? _spyInstance;
        private readonly DotNetObjectReference<OutlineScrollSpy> _dotNetRef;
        private bool _disposed;

        /// <summary>
        /// The id of the currently centered section
        /// </summary>
        public string? CenteredSection { get; private set; }

        /// <summary>
        /// Event raised when a section is centered
        /// </summary>
        public event EventHandler<ScrollSectionCenteredEventArgs>? ScrollSectionSectionCentered;

        /// <summary>
        /// Initialize the class by supplying the IJSRuntime
        /// </summary>
        /// <param name="js"></param>
        public OutlineScrollSpy(IJSRuntime js)
        {
            _js = js;
            _dotNetRef = DotNetObjectReference.Create(this);
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
            _spyInstance = await _js.InvokeAsync<IJSObjectReference>("createScrollSpy");
            await _spyInstance.InvokeVoidAsync("spying", _dotNetRef, containerSelector, sectionClassSelector);
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
            CenteredSection = id;
            if (_spyInstance is not null)
                await _spyInstance.InvokeVoidAsync("scrollToSection", id.Trim('#'));
        }

        /// <summary>
        /// Sets a section as active by appending a # and id to the history (Current Navigation URL). Scrolling does not occur.
        /// </summary>
        /// <param name="id">The id of the section</param>
        public async Task SetSectionAsActive(string id)
        {
            CenteredSection = id;
            if (_spyInstance is not null)
                await _spyInstance.InvokeVoidAsync("activateSection", id.Trim('#'));
        }

        /// <summary>
        /// Raises the ScrollSectionSectionCentered event with the id of the centered section. Typically called in javascript.
        /// </summary>
        /// <param name="id">The id of the centered section</param>
        [JSInvokable]
        public void SectionChangeOccured(string id)
        {
            CenteredSection = id;
            ScrollSectionSectionCentered?.Invoke(this, new ScrollSectionCenteredEventArgs(id));
        }

        /// <summary>
        /// Dispose the scrollspy, dotnetref, and js
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;

                if (_spyInstance is not null)
                {
                    await _spyInstance.InvokeVoidAsync("unspy");
                    await _spyInstance.DisposeAsync();
                }

                _dotNetRef.Dispose();
            }
        }
    }

}
