namespace MudX.Docs.Data
{
    /// <summary>
    /// Represents a member of an API, such as a parameter, property, method, field, or event.
    /// </summary>
    public class ApiMember
    {
        /// <summary>
        /// Gets or sets the name of the API member.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the API member.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the API member.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the default value of the API member, if applicable.
        /// </summary>
        public string? Default { get; set; }
    }
}
