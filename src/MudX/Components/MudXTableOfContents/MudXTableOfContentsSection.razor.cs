using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MudX
{
    public partial class MudXTableOfContentsSection : MudComponentBase
    {
        private readonly List<MudXTableOfContentsSection> _children = [];
        internal string _id;

        public MudXTableOfContentsSection()
        {

        }

        internal MudXTableOfContentsSection? Parent { get; }

        internal int Level => Parent?.Level + 1 ?? 0;

        internal int LevelSortingValue { get; private set; }

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
        /// Indicating if the section is currently in the middle of the viewport
        /// </summary>
        public bool Active { get; private set; }

        protected override void OnParametersSet()
        {
            _id = Id ?? GetId();
            base.OnParametersSet();
        }

        /// <summary>
        ///  turn title lowercase, and replace spaces with hyphens
        /// </summary>
        /// <returns></returns>
        private string GetId() => Title?.ToLower().Replace(" ", "-") ?? Guid.NewGuid().ToString();

        protected internal void Activate() => Active = true;

        protected internal void Deactive() => Active = false;

        internal void SetLevelStructure(int counter, int diff)
        {
            LevelSortingValue = counter;
            var levelDiff = diff / 10;
            var value = counter + levelDiff;
            foreach (var item in _children)
            {
                item.SetLevelStructure(value, levelDiff);
                value += levelDiff;
            }
        }
    }
}
