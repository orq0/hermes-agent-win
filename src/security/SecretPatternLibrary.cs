namespace Hermes.Agent.Security;

/// <summary>
/// Collection of all secret detection patterns, composed by SecretDetector.
/// Each pattern is independently testable.
/// </summary>
public static class SecretPatternLibrary
{
    public static IReadOnlyList<ISecretPattern> All => _patterns;

    private static readonly IReadOnlyList<ISecretPattern> _patterns = new List<ISecretPattern>
    {
        new KnownPrefixPattern(),
        new AuthorizationHeaderPattern(),
        new JsonFieldPattern(),
        new PrivateKeyPattern(),
        new DbConnStrPattern(),
        new UrlCredentialPattern(),
    };

 }

/// <summary>
/// Detects known API key prefix patterns (OpenAI, GitHub, Slack, AWS, Stripe, etc.).
/// </summary>
public sealed class KnownPrefixPattern : RegexSecretPattern
{
    private static readonly string[] Prefixes =
    {
        @"sk-[A-Za-z0-9_-]{10,}",
        @"ghp_[A-Za-z0-9]{10,}",
        @"github_pat_[A-Za-z0-9_]{10,}",
        @"gho_[A-Za-z0-9]{10,}",
        @"ghu_[A-Za-z0-9_]{10,}",
        @"ghs_[A-Za-z0-9]{10,}",
        @"ghr_[A-Za-z0-9]{10,}",
        @"xox[baprs]-[A-Za-z0-9-]{10,}",
        @"AIza[A-Za-z0-9_-]{30,}",
        @"pplx-[A-Za-z0-9]{10,}",
        @"AKIA[A-Z0-9]{16}",
        @"sk_live_[A-Za-z0-9]{10,}",
        @"sk_test_[A-Za-z0-9]{10,}",
        @"rk_live_[A-Za-z0-9]{10,}",
        @"SG\.[A-Za-z0-9_-]{10,}",
        @"hf_[A-Za-z0-9]{10,}",
        @"r8_[A-Za-z0-9]{10,}",
        @"npm_[A-Za-z0-9]{10,}",
        @"pypi-[A-Za-z0-9_-]{10,}",
        @"tvly-[A-Za-z0-9]{10,}",
        @"exa_[A-Za-z0-9]{10,}",
    };

    private static readonly string RawPattern =
        @"(?<![A-Za-z0-9_-])(" + string.Join("|", Prefixes) + @")(?![A-Za-z0-9_-])";

    private static readonly Regex _regex = new(RawPattern, RegexOptions.Compiled);

    public KnownPrefixPattern() : base("KnownPrefix", RawPattern, RegexOptions.Compiled) { }

    public override bool Matches(string text) => _regex.IsMatch(text);

    public override string ApplyRedaction(string text)
    {
        return _regex.Replace(text, m => MaskToken(m.Value));
    }

    private static string MaskToken(string token)
    {
        if (token.Length < 18)
            return "[REDACTED]";
        return $"{token[..6]}...[REDACTED]";
    }
}

/// <summary>
/// Detects Authorization: Bearer headers.
/// </summary>
public sealed class AuthorizationHeaderPattern : RegexSecretPattern
{
    private static readonly Regex _regex = new(
        @"(Authorization:\s*Bearer\s+)(\S+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public AuthorizationHeaderPattern() : base("AuthHeader", _regex.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase) { }

    public override bool Matches(string text) => _regex.IsMatch(text);

    public override string ApplyRedaction(string text)
    {
        return _regex.Replace(text, m => $"{m.Groups[1].Value}[REDACTED]");
    }
}

/// <summary>
/// Detects JSON fields containing secret values (api_key, token, secret, password, etc.).
/// </summary>
public sealed class JsonFieldPattern : RegexSecretPattern
{
    private static readonly Regex _regex = new(
        @"(""(?:api_?[Kk]ey|token|secret|password|access_token|refresh_token|auth_token|bearer)"")\s*:\s*""([^""]+)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public JsonFieldPattern() : base("JsonField", _regex.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase) { }

    public override bool Matches(string text) => _regex.IsMatch(text);

    public override string ApplyRedaction(string text)
    {
        return _regex.Replace(text, m => $"{m.Groups[1].Value}: \"[REDACTED]\"");
    }
}

/// <summary>
/// Detects PEM private key blocks.
/// </summary>
public sealed class PrivateKeyPattern : RegexSecretPattern
{
    private static readonly Regex _regex = new(
        @"-----BEGIN[A-Z ]*PRIVATE KEY-----[\s\S]*?-----END[A-Z ]*PRIVATE KEY-----",
        RegexOptions.Compiled);

    public PrivateKeyPattern() : base("PrivateKey", _regex.ToString(), RegexOptions.Compiled) { }

    public override bool Matches(string text) => _regex.IsMatch(text);

    public override string ApplyRedaction(string text)
    {
        return _regex.Replace(text, "[REDACTED PRIVATE KEY]");
    }
}

/// <summary>
/// Detects database connection string passwords.
/// </summary>
public sealed class DbConnStrPattern : RegexSecretPattern
{
    private static readonly Regex _regex = new(
        @"((?:postgres(?:ql)?|mysql|mongodb(?:\+srv)?|redis|amqp)://[^:]+:)([^@]+)(@)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public DbConnStrPattern() : base("DbConnStr", _regex.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase) { }

    public override bool Matches(string text) => _regex.IsMatch(text);

    public override string ApplyRedaction(string text)
    {
        return _regex.Replace(text, m => $"{m.Groups[1].Value}[REDACTED]{m.Groups[3].Value}");
    }
}

/// <summary>
/// Detects URLs with embedded credentials (user:pass@host).
/// </summary>
public sealed class UrlCredentialPattern : RegexSecretPattern
{
    private static readonly Regex _regex = new(
        @"(https?://[^:]+:)([^@]+)(@[^/\s]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public UrlCredentialPattern() : base("UrlCredential", _regex.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase) { }

    public override bool Matches(string text) => _regex.IsMatch(text);

    public override string ApplyRedaction(string text)
    {
        return _regex.Replace(text, m => $"{m.Groups[1].Value}[REDACTED]{m.Groups[3].Value}");
    }
}
