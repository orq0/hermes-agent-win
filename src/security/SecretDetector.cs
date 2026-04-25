namespace Hermes.Agent.Security;

/// <summary>
/// Non-static implementation of ISecretDetector that composes SecretPatternLibrary patterns.
/// This is the deepened module — patterns are injected, not hardcoded.
/// </summary>
public class SecretDetector : ISecretDetector
{
    private readonly IReadOnlyList<ISecretPattern> _patterns;

    public SecretDetector() : this(SecretPatternLibrary.All) { }

    internal SecretDetector(IReadOnlyList<ISecretPattern> patterns)
    {
        _patterns = patterns;
    }

    public bool ContainsSecrets(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        foreach (var pattern in _patterns)
        {
            if (pattern.Matches(text)) return true;
        }

        return false;
    }

    public string RedactSecrets(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";

        var result = text;
        foreach (var pattern in _patterns)
        {
            result = pattern.ApplyRedaction(result);
        }

        return result;
    }

    public bool UrlContainsCredentials(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;

        // Check URL credential pattern and DB connection string pattern specifically
        foreach (var pattern in _patterns)
        {
            if ((pattern is UrlCredentialPattern || pattern is DbConnStrPattern) && pattern.Matches(url))
                return true;
        }

        return false;
    }
}
