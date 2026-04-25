namespace Hermes.Agent.Security;

/// <summary>
/// Static facade for SecretDetector.
/// Provides backward-compatible static methods while the real implementation lives in SecretDetector.
/// </summary>
public static class SecretScanner
{
    private static readonly SecretDetector _detector = new();

    /// <summary>Check if text contains any detectable secrets.</summary>
    public static bool ContainsSecrets(string? text) => _detector.ContainsSecrets(text);

    /// <summary>
    /// Redact all detected secrets in the text, replacing them with [REDACTED].
    /// Safe to call on any string. Non-matching text passes through unchanged.
    /// </summary>
    public static string RedactSecrets(string? text) => _detector.RedactSecrets(text);

    /// <summary>
    /// Scan a URL for embedded credentials. Returns true if credentials are found.
    /// </summary>
    public static bool UrlContainsCredentials(string? url) => _detector.UrlContainsCredentials(url);
}
