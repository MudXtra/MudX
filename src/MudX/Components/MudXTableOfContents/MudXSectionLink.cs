namespace MudX.Components.MudXTableOfContents
{
    public class MudXSectionLink
    {
        /// <summary>
        /// The id of the section, can be overridden
        /// </summary>
        public string Id { get; set; } = $"mudx-sectonid-{Guid.NewGuid()}";

        public int Order { get; set; } = -1;

        public string Title { get; set; } = default!;

        public bool Active { get; set; }
    }
}
