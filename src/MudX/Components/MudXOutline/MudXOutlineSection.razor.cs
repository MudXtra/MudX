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

        protected override void OnParametersSet()
        {
            if (string.IsNullOrWhiteSpace(SectionId))
            {
                var sectionId = Id ?? GetId();
                SectionId = sectionId;
                if (Root is MudXOutline outline)
                {
                    outline.BuildSectionIdsUnique();
                }
            }
            base.OnParametersSet();
        }

        private IOutlineContainer? Root
        {
            get
            {
                var current = this;
                while (current?.ParentContainer is not null)
                {
                    current = current.ParentContainer as MudXOutlineSection;
                }
                return current;
            }
        }

        /// <summary>
        ///  turn title lowercase, and replace spaces with hyphens
        /// </summary>
        /// <returns></returns>
        private string GetId() => Title?.ToLower().Replace(" ", "-") ?? Guid.NewGuid().ToString();

        protected internal void Activate() => Active = true;

        protected internal void Deactive() => Active = false;

        protected override async Task OnInitializedAsync()
        {
            if (ParentContainer != null)
            {
                await ParentContainer.RegisterSectionAsync(this);
            }
            await base.OnInitializedAsync();
        }

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
