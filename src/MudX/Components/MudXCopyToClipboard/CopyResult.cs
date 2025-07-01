namespace MudX
{
    /// <summary>
    /// Represents the result of a copy operation, including its success status and an associated message.
    /// </summary>
    /// <remarks>This class provides information about the outcome of a copy operation. The <see
    /// cref="Success"/> property indicates whether the operation was successful, and the <see cref="Message"/> property
    /// contains a descriptive message about the result.</remarks>
    public class CopyResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        /// <remarks>Defaults to <c>false</c>.</remarks>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Gets or sets the Message returned with the CopyResult
        /// </summary>
        /// <remarks>Defaults to "Not initialized.".</remarks>
        public string Message { get; set; } = "Not initialized.";
    }
}
