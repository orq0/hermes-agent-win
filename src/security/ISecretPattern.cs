namespace Hermes.Agent.Security;

/// <summary>
/// A single secret detection pattern that can match and redact.
/// </summary>
public interface ISecretPattern
{
    /// <summary>Human-readable name for this pattern.</summary>
    string Name { get; }

    /// <summary>Whether text matches this pattern.</summary>
    bool Matches(string text);

    /// <summary>Apply redaction to text for this pattern.</summary>
    string ApplyRedaction(string text);
}

/// <summary>
/// Base class for regex-based secret patterns.
/// </summary>
public abstract class RegexSecretPattern : ISecretPattern
{
    public string Name { get; }
    private readonly Regex _regex;

    protected RegexSecretPattern(string name, string pattern, RegexOptions options = RegexOptions.Compiled)
    {
        Name = name;
        _regex = new Regex(pattern, options);
    }

    public bool Matches(string text) => _regex.IsMatch(text);

    public abstract string ApplyRedaction(string text);
}
