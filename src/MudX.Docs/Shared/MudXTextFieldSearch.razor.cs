using System.Text.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.State;
using MudBlazor.Utilities;
using MudX.Models;

namespace MudX.Docs.Shared
{
    /// <summary>
    /// Search Box and Popover for Searching Docs and Website Content.
    /// </summary>
    public partial class MudXTextFieldSearch : MudComponentBase
    {
        private readonly ParameterState<bool> _openState;
        private List<NavItem> _navList = [];
        private List<NavItem> _searchResults = [];
        private NavItem? _searchItem;
        private MudAutocomplete<NavItem>? _autoComplete;

        /// <summary>
        /// Constructor for TextSearch component.
        /// </summary>
        public MudXTextFieldSearch()
        {
            using var registerScope = CreateRegisterScope();
            _openState = registerScope.RegisterParameter<bool>(nameof(Open))
                        .WithParameter(() => Open)
                        .WithEventCallback(() => OpenChanged);
        }

        [Inject]
        private NavigationManager NavManager { get; set; } = default!;

        /// <summary>
        /// Whether or not the TextSearch is visible
        /// </summary>
        [Parameter]
        public bool Open { get; set; }

        /// <summary>
        /// Gets or sets the callback that is invoked when the open state changes.
        /// </summary>
        [Parameter]
        public EventCallback<bool> OpenChanged { get; set; }

        /// <summary>
        /// Gets the CSS class name for the background textbox, including any additional classes specified by the <see
        /// cref="Class"/> property.
        /// </summary>
        protected string Classname =>
            new CssBuilder("background-textbox")
            .AddClass(Class)
            .Build();

        /// <summary>
        /// Override OnInitialized
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();
            _navList = GeneratedNavList();
        }

        private bool PopoverOpen => _navList.Count > 0 && Open;

        private async Task<IEnumerable<NavItem>> Search(string searchText, CancellationToken token)
        {
            if (searchText is null)
                searchText = string.Empty;
            _searchResults = [.. _navList
                    .Where(nav => nav.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                  (nav.Action != null && nav.Action.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                  (nav.Application != null && nav.Application.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                  (nav.Controller != null && nav.Controller.Contains(searchText, StringComparison.OrdinalIgnoreCase)))];
            await Task.CompletedTask;
            return _searchResults;
        }

        private async Task SelectSearchResult()
        {
            if (_searchItem == null)
                return;
            await SelectSearchResult(_searchItem);
        }

        private async Task SelectSearchResult(NavItem result)
        {
            _searchItem = default;
            NavManager.NavigateTo(result.Route);
            if (_autoComplete != null)
                await _autoComplete.CloseMenuAsync();
            StateHasChanged();
        }

        private static List<NavItem> GeneratedNavList()
        {
            var navItems = new List<NavItem> {
            // Parent Levels (Static)
            new() { NavItemId=90, Title="Home", Action="", OrderById=0, IsActive=true },
            new() { NavItemId=91, Title="Install", Action="installation", OrderById=1, IsActive=true },
            //new NavItem() { NavItemId=99, Title="About", Action="about", OrderById=99, IsActive=true },
        };

            // load generated docs
            var assembly = typeof(MainLayout).Assembly;
            var resourceName = "MudX.Docs.wwwroot.nav-structure.json"; // Adjust if needed
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream!);
            var json = reader.ReadToEnd();
            var navStructure = JsonSerializer.Deserialize<List<NavItem>>(json) ?? [];
            navItems.AddRange(navStructure);

            return [.. navItems
                .OrderBy(m => m.NavItemId)
                .ThenBy(m => !m.ParentId.HasValue ? int.MaxValue : m.ParentId) // Handle null ParentId which will ensure parent's are sorted by orderbyId
                .ThenBy(m => m.OrderById)];
        }
    }
}