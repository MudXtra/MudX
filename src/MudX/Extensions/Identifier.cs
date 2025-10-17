namespace MudX.Extensions;

internal static class Identifier
{
    private const string Chars = "abcdefghijklmnopqrstuvwxyz0123456789";

    private const int CharsLength = 35;

    private const int RandomStringLength = 8;

    //
    // Summary:
    //     Creates a unique identifier with the specified prefix.
    //
    // Parameters:
    //   prefix:
    //     The prefix to prepend to the unique identifier.
    //
    // Returns:
    //     A unique identifier string with the specified prefix.
    internal static string Create(ReadOnlySpan<char> prefix)
    {
        Span<char> destination = stackalloc char[prefix.Length + 8];
        prefix.CopyTo(destination);
        for (int i = 0; i < 8; i++)
        {
            int index = Random.Shared.Next(35);
            destination[prefix.Length + i] = "abcdefghijklmnopqrstuvwxyz0123456789"[index];
        }

        return destination.ToString();
    }
}
