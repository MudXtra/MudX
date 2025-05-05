using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace MudX.Components
{
    /// <summary>
    /// Extends MudBreadcrumb to automatically listen to route changes and update the breadcrumbs to a label and optional route.
    /// 
    /// </summary>
    /// <remarks>Should be placed in MainLayout or similar</remarks>
    public class MudXBreadcrumbsBase : MudBreadcrumbs, IDisposable
    {
        [Inject] private NavigationManager NavManager { get; set; } = default!;

        private readonly List<BreadcrumbItem> _items = [];

        /// <summary>
        /// Optional function to format each segment label.
        /// </summary>
        [Parameter]
        public Func<string, string>? FormatFunc { get; set; }

        /// <summary>
        /// The text to display for the home link ( or "/" ).
        /// </summary>
        /// <remarks>Defaults to "Home".</remarks>
        [Parameter]
        public string HomeText { get; set; } = "Home";

        /// <summary>
        /// Read-only list of BreadcrumbItems, hidden from Parameters
        /// </summary>
        public new IReadOnlyList<BreadcrumbItem>? Items => base.Items;

        protected override void OnInitialized()
        {
            base.Items = [new BreadcrumbItem(HomeText, "/", true)];
            NavManager.LocationChanged += OnLocationChanged;
            base.OnInitialized();
        }

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
            _items.Clear();

            if (string.IsNullOrEmpty(uri))
            {
                _items.Add(new BreadcrumbItem(HomeText, "/", true));
            }
            else
            {
                var segments = uri.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var path = "";
                for (int i = 0; i < segments.Length; i++)
                {
                    path += "/" + segments[i];
                    var isCurrent = i == segments.Length - 1;
                    var label = FormatFunc?.Invoke(segments[i]) ?? FormatLabel(segments[i]);
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

        public void Dispose()
        {
            NavManager.LocationChanged -= OnLocationChanged;
        }
    }
}
