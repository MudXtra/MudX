namespace MudX
{
    /// <summary>
    /// Represents a code file with associated metadata, including its title, content, and programming language.
    /// </summary>
    /// <remarks>This record is immutable and provides a convenient way to encapsulate information about a
    /// code file. It can be used to store or transfer code snippets along with their metadata.</remarks>
    /// <param name="Title">The title or name of the code file. Cannot be null or empty.</param>
    /// <param name="Code">The content of the code file, typically the source code. Cannot be null or empty.</param>
    /// <param name="Language">The programming language of the code file, represented as a <see cref="CodeLanguage"/> value.</param>
    public record CodeFile(string Title, string Code, CodeLanguage Language);
}
