using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MudX.Models
{
    public class NavItem
    {
        [Key]
        public int NavItemId { get; set; }
        public int OrderById { get; set; }
        public string Title { get; set; } = default!;
        public string? Icon { get; set; }
        public string? Application { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public string? Role { get; set; }
        public string? OverrideHref { get; set; }
        public bool IsNewWindow { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // Navigation property for child links
        public List<NavItem> ChildNavItems { get; set; } = [];

        // Foreign key for parent link
        public int? ParentId { get; set; }

        // Foreign key attribute
        [ForeignKey("ParentId")]
        public virtual NavItem? ParentNavItem { get; set; }

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
