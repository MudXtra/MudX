using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using MudBlazor.State;

namespace MudX.Components
{
    /// <summary>
    /// Extends MudBreadcrumb to automatically listen to route changes and update the breadcrumbs to a label and optional route.
    /// 
    /// </summary>
    /// <remarks>Should be placed in MainLayout or similar</remarks>
    public class MudXBreadcrumbsBase : MudBreadcrumbs, IDisposable
    {
        private ParameterState<string> _homeTextState;

        /// <summary>
        /// Initializes a new instance of the <see cref="MudBreadcrumbs"/> class in MudX.
        /// </summary>
        /// <remarks>This constructor sets up the internal state for managing the <c>HomeText</c>
        /// parameter, including registering its change handler and event callback. It additionally 
        /// injects NavigationManager to track movements.</remarks>
        public MudXBreadcrumbsBase()
        {
            using var registerScope = CreateRegisterScope();
            _homeTextState = registerScope.RegisterParameter<string>(nameof(HomeText))
                .WithParameter(() => HomeText)
                .WithEventCallback(() => HomeTextChanged)
                .WithChangeHandler(HomeTextHandleChanged);
        }

        [Inject] private NavigationManager NavManager { get; set; } = default!;

        /// <summary>
        /// The text to display for the home link ( or "/" ).
        /// </summary>
        /// <remarks>Defaults to "Home".</remarks>
        [Parameter]
        public string HomeText { get; set; } = "Home";

        /// <summary>
        /// Event callback for HomeText changes.
        /// </summary>
        [Parameter]
        public EventCallback<string> HomeTextChanged { get; set; }

        private void HomeTextHandleChanged(ParameterChangedEventArgs<string> args) => BuildBreadcrumbs();

        /// <summary>
        /// Read-only list of BreadcrumbItems, hidden from Parameters
        /// </summary>
        public new IReadOnlyList<BreadcrumbItem>? Items => base.Items;

        /// <summary>
        /// OnInitialized override
        /// </summary>
        protected override void OnInitialized()
        {
            base.Items = [new BreadcrumbItem(HomeText, "/", true)];
            NavManager.LocationChanged += OnLocationChanged;
            base.OnInitialized();
        }

        /// <summary>
        /// OnAfterRender override
        /// </summary>
        /// <param name="firstRender"></param>
        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                BuildBreadcrumbs();
            }

            base.OnAfterRender(firstRender);
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            BuildBreadcrumbs();
        }

        private void BuildBreadcrumbs()
        {
            var uri = NavManager.ToBaseRelativePath(NavManager.Uri);
            var _items = new List<BreadcrumbItem>();
            _items.Add(new BreadcrumbItem(HomeText, "/", string.IsNullOrEmpty(uri)));
            if (!string.IsNullOrEmpty(uri))
            {
                var segments = uri.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var path = "";
                for (int i = 0; i < segments.Length; i++)
                {
                    path += "/" + segments[i];
                    var isCurrent = i == segments.Length - 1;
                    var label = FormatLabel(segments[i]);
                    _items.Add(new BreadcrumbItem(label, path, isCurrent));
                }
            }

            base.Items = _items;
            InvokeAsync(StateHasChanged);
        }

        private static string FormatLabel(string segment)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(segment.Replace("-", " ").Replace("_", " "));
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            NavManager.LocationChanged -= OnLocationChanged;
        }
    }
}
