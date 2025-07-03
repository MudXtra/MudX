using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudX.Components.MudXOutline;

namespace MudX
{
    /// <summary>
    /// Represents a section within an outline or table of contents, providing hierarchical structure and navigation
    /// capabilities.
    /// </summary>
    /// <remarks>This class is used to define sections in a document outline, supporting features such as
    /// hierarchical levels, scrolling behavior, and active state tracking. Sections can be nested within parent
    /// containers, and their properties such as <see cref="Title"/> and <see cref="Id"/> are used for display and
    /// navigation.</remarks>
    public partial class MudXOutlineSection : MudComponentBase, IOutlineContainer
    {
        internal string _id = Guid.NewGuid().ToString();
        internal readonly List<MudXOutlineSection> _subSections = [];

        /// <summary>
        /// Gets the hierarchical level of the current item within its parent container.
        /// </summary>
        public int Level => ParentContainer?.Level + 1 ?? 0;

        private MudXOutline? _outline;

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

        /// <summary>
        /// The SectionID used for this section
        /// </summary>
        public string SectionId { get; internal set; } = string.Empty;

        /// <summary>
        /// Indicating if the section is currently in the middle of the viewport
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// OnInitializedAsync override
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (ParentContainer != null)
            {
                SetParentOutline(ParentContainer);
                _outline?.RegisterUniqueIds(this);
                await ParentContainer.RegisterSectionAsync(this);
            }
        }

        private void SetParentOutline(IOutlineContainer? outlineContainer)
        {
            var current = outlineContainer;
            while (current != null)
            {
                if (current is MudXOutline outline)
                {
                    _outline = outline;
                    return;
                }
                if (current is MudXOutlineSection section)
                {
                    current = section.ParentContainer;
                }
                else
                {
                    break;
                }
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

        /// <summary>
        /// Activates the current section by setting its state to active.
        /// </summary>
        protected internal void Activate() => Active = true;

        /// <summary>
        /// Deactivates the current section, setting its active state to false.
        /// </summary>
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
