namespace Hermes.Agent.Security;

/// <summary>
/// Detects and redacts secrets in text content.
/// </summary>
public interface ISecretDetector
{
    /// <summary>Check if text contains any detectable secrets.</summary>
    bool ContainsSecrets(string? text);

    /// <summary>Redact all detected secrets in the text.</summary>
    string RedactSecrets(string? text);

    /// <summary>Scan a URL for embedded credentials.</summary>
    bool UrlContainsCredentials(string? url);
}
