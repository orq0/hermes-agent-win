namespace Hermes.Agent.Dreamer;

internal static class DreamerSlug
{
    /// <summary>Sanitize slug to prevent path traversal attacks and invalid project names.</summary>
    public static string Sanitize(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return "";

        // Remove dangerous path sequences.
        var result = slug.Replace("..", "")
                         .Replace("/", "-")
                         .Replace("\\", "-");

        // Remove invalid filename chars.
        foreach (var c in Path.GetInvalidFileNameChars())
            result = result.Replace(c.ToString(), "");

        // Ensure not rooted after normalization.
        if (Path.IsPathRooted(result))
            return "";

        return result.Trim();
    }
}
