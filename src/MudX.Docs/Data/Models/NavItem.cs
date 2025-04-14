using System.ComponentModel.DataAnnotations.Schema;

namespace MudX.Docs.Data.Models
{
    internal class NavItem
    {
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

        public NavItem()
        {

        }
        public NavItem(NavItem item)
        {
            NavItemId = item.NavItemId;
            OrderById = item.OrderById;
            Title = item.Title;
            Icon = item.Icon;
            Application = item.Application;
            Controller = item.Controller;
            Action = item.Action;
            Role = item.Role;
            OverrideHref = item.OverrideHref;
            IsNewWindow = item.IsNewWindow;
            IsActive = item.IsActive;
            ParentId = item.ParentId ?? -1;
        }
        public string Route
        {
            get
            {
                string href = string.IsNullOrWhiteSpace(OverrideHref) ? string.Empty : OverrideHref;
                if (string.IsNullOrWhiteSpace(href))
                {
                    href = "/";
                    var segments = new List<string>();
                    if (!string.IsNullOrWhiteSpace(Application)) segments.Add(Application);
                    if (!string.IsNullOrWhiteSpace(Controller)) segments.Add(Controller);
                    if (!string.IsNullOrWhiteSpace(Action)) segments.Add(Action);
                    href += segments.Count > 0 ? string.Join("/", segments) : string.Empty;
                }

                return string.IsNullOrWhiteSpace(href) ? "/" : href;
            }
        }
    }
}
