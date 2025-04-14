using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MudX
{
    /// <summary>
    /// Extends MudBreadcrumb to automatically listen to route changes and update the breadcrumbs to a label and optional route.
    /// 
    /// </summary>
    /// <remarks>Should be placed in MainLayout or similar</remarks>
    public partial class MudXBreadcrumbs : MudComponentBase
    {
        internal List<BreadcrumbItem> crumbs = [];

        [Inject]
        private NavigationManager NavManager { get; set; } = default!;

        /// <summary>
        /// The separator shown between items.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>/</c>.  Will not be shown if <see cref="SeparatorTemplate"/> is set.
        /// </remarks>
        [Parameter]
        public string Separator { get; set; } = "/";

        /// <summary>
        /// The content shown between items.
        /// </summary>
        [Parameter]
        public RenderFragment? SeparatorTemplate { get; set; }


        /// <summary>
        /// The custom template used to display items.
        /// </summary>
        [Parameter]
        public RenderFragment<BreadcrumbItem>? ItemTemplate { get; set; }

        /// <summary>
        /// The maximum number of items to display.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>null</c>. When the number of items exceeds this value, the breadcrumbs will automatically collapse.
        /// </remarks>
        [Parameter]
        [Category(CategoryTypes.Breadcrumbs.Behavior)]
        public byte? MaxItems { get; set; }

        /// <summary>
        /// The icon to display when items are collapsed.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Icons.Material.Filled.SettingsEthernet" />.  Displays when the number of items exceeds <see cref="MaxItems"/>.
        /// </remarks>
        [Parameter]
        [Category(CategoryTypes.Breadcrumbs.Appearance)]
        public string ExpanderIcon { get; set; } = Icons.Material.Filled.SettingsEthernet;

        /// <summary>
        /// Optional function to format each segment label.
        /// </summary>
        [Parameter]
        public Func<string, string>? FormatFunc { get; set; }


        protected override void OnInitialized()
        {
            NavManager.LocationChanged += (sender, e) => BuildBreadcrumbs();
            BuildBreadcrumbs();

            base.OnInitialized();
        }

        private void BuildBreadcrumbs()
        {
            var uri = NavManager.ToBaseRelativePath(NavManager.Uri);
            crumbs.Clear();

            if (string.IsNullOrEmpty(uri))
            {
                crumbs.Add(new BreadcrumbItem("Home", "/", true));
                return;
            }

            var segments = uri.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var path = "";
            for (int i = 0; i < segments.Length; i++)
            {
                path += "/" + segments[i];
                var isCurrent = i == segments.Length - 1;
                var label = FormatFunc?.Invoke(segments[i]) ?? FormatLabel(segments[i]);
                crumbs.Add(new BreadcrumbItem(label, path, isCurrent));
            }

            StateHasChanged();
        }

        private static string FormatLabel(string segment)
        {
            // You can improve this — map route values to pretty names, etc.
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(segment.Replace("-", " ").Replace("_", " "));
        }
    }
}
