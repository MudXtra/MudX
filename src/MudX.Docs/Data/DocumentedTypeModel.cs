namespace MudX.Docs.Data
{
    /// <summary>
    /// Represents a documented type, including its metadata and API members.
    /// </summary>
    public class DocumentedTypeModel
    {
        /// <summary>
        /// Gets or sets the name of the component or type.
        /// </summary>
        public string Component { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the namespace of the component or type.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the component or type.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the list of parameter members for the component or type.
        /// </summary>
        public List<ApiMember> Parameters { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of property members for the component or type.
        /// </summary>
        public List<ApiMember> Properties { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of method members for the component or type.
        /// </summary>
        public List<ApiMember> Methods { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of field members for the component or type.
        /// </summary>
        public List<ApiMember> Fields { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of event members for the component or type.
        /// </summary>
        public List<ApiMember> Events { get; set; } = new();
    }
}
