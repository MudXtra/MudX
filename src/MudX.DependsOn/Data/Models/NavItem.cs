using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MudX.Models
{
    /// <summary>
    /// Represents a navigation item in a hierarchical menu structure.
    /// </summary>
    /// <remarks>
    /// A navigation item can define properties such as its display title, icon, route, and
    /// role-based access. It supports hierarchical relationships, allowing child navigation items to be associated with
    /// a parent. The <see cref="Route"/> property dynamically generates the URL for the navigation item based on its
    /// <see cref="Application"/>, <see cref="Controller"/>, and <see cref="Action"/> properties, or uses <see
    /// cref="OverrideHref"/> if specified.
    /// </remarks>
    public class NavItem
    {
        /// <summary>
        /// Gets or sets the unique identifier for the navigation item.
        /// </summary>
        [Key]
        public int NavItemId { get; set; }

        /// <summary>
        /// Gets or sets the order index for sorting navigation items.
        /// </summary>
        public int OrderById { get; set; }

        /// <summary>
        /// Gets or sets the display title of the navigation item.
        /// </summary>
        public string Title { get; set; } = default!;

        /// <summary>
        /// Gets or sets the icon associated with the navigation item.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Gets or sets the application segment of the route.
        /// </summary>
        public string? Application { get; set; }

        /// <summary>
        /// Gets or sets the controller segment of the route.
        /// </summary>
        public string? Controller { get; set; }

        /// <summary>
        /// Gets or sets the action segment of the route.
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// Gets or sets the role required to access the navigation item.
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// Gets or sets an optional override URL for the navigation item.
        /// </summary>
        public string? OverrideHref { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the navigation item should open in a new window.
        /// </summary>
        public bool IsNewWindow { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the navigation item is active and visible.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the collection of child navigation items.
        /// </summary>
        public List<NavItem> ChildNavItems { get; set; } = [];

        /// <summary>
        /// Gets or sets the parent navigation item's identifier, if any.
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Gets or sets the parent navigation item.
        /// </summary>
        [ForeignKey("ParentId")]
        public virtual NavItem? ParentNavItem { get; set; }

        /// <summary>
        /// Gets the computed route for the navigation item.
        /// Prefers <see cref="OverrideHref"/> if set; otherwise, builds the route from <see cref="Application"/>, <see cref="Controller"/>, and <see cref="Action"/>.
        /// Returns "/" if no segments are specified.
        /// </summary>
        public string Route
        {
            get
            {
                // Prefer OverrideHref if it is set
                if (!string.IsNullOrWhiteSpace(OverrideHref))
                    return OverrideHref;

                // Build route from Application, Controller, Action
                var segments = new List<string>();
                if (!string.IsNullOrWhiteSpace(Application)) segments.Add(Application);
                if (!string.IsNullOrWhiteSpace(Controller)) segments.Add(Controller);
                if (!string.IsNullOrWhiteSpace(Action)) segments.Add(Action);

                // If no segments, return home "/"
                if (segments.Count == 0)
                    return "/";

                // Otherwise, combine segments with "/" prefix
                return "/" + string.Join("/", segments);
            }
        }

    }
}
