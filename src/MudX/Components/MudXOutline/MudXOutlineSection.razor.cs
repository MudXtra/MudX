using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudX.Components.MudXOutline;

namespace MudX
{
    public partial class MudXOutlineSection : MudComponentBase, IOutlineContainer
    {
        internal string _id = Guid.NewGuid().ToString();
        internal readonly List<MudXOutlineSection> _subSections = [];

        public int Level => ParentContainer?.Level + 1 ?? 0;

        internal int LevelSortingValue { get; private set; }

        [CascadingParameter]
        internal IOutlineContainer? ParentContainer { get; set; }

        /// <summary>
        /// The title of the section in the Table of Contents.
        /// </summary>
        [Parameter, EditorRequired]
        public string Title { get; set; } = default!;

        /// <summary>
        /// The id of the section, used for scrolling. This will be what is appended to the current url, if the section becomes active.
        /// </summary>
        /// <remarks>(optional) Defaults to the title</remarks>
        [Parameter]
        public string? Id { get; set; }

        /// <summary>
        /// The content of the Document Sections
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        public string SectionId { get; internal set; } = string.Empty;

        /// <summary>
        /// Indicating if the section is currently in the middle of the viewport
        /// </summary>
        public bool Active { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (ParentContainer != null)
            {
                await ParentContainer.RegisterSectionAsync(this);
            }
        }

        /// <summary>
        /// Generates a ScrollSpy-compatible ID from the Id or Title property.
        /// </summary>
        /// <returns>A sanitized, lowercase ID safe for ScrollSpy and HTML anchors.</returns>
        internal string GetId()
        {
            var id = string.IsNullOrWhiteSpace(Id) ? Title : Id;

            if (string.IsNullOrWhiteSpace(id))
            {
                return "section";
            }

            // Convert to lowercase, trim, and replace invalid characters
            id = id.Trim().ToLowerInvariant();
            id = System.Text.RegularExpressions.Regex.Replace(id, @"[^a-z0-9\-_:]", "-");

            // Ensure ID doesn't start with a digit or hyphen
            if (string.IsNullOrEmpty(id) || char.IsDigit(id[0]) || id[0] == '-')
            {
                id = "section-" + id;
            }

            return id;
        }

        protected internal void Activate() => Active = true;

        protected internal void Deactivate() => Active = false;

        internal void SetLevelStructure(int counter = 0, int diff = 1000)
        {
            LevelSortingValue = counter;
            int childDiff = diff / 10;
            int childCounter = counter + childDiff;

            foreach (var child in _subSections)
            {
                child.SetLevelStructure(childCounter, childDiff);
                childCounter += childDiff;
            }
        }

        /// <summary>
        /// Adds a section to the Table of Contents
        /// </summary>
        public async Task RegisterSectionAsync(MudXOutlineSection section)
        {
            // Add section logic
            _subSections.Add(section);
            await Task.CompletedTask;
        }
    }
}
