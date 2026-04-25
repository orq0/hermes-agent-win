namespace HermesDesktop.Tests.Security;

using Hermes.Agent.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SecretDetectorTests
{
    [TestMethod]
    public void ContainsSecrets_NullText_ReturnsFalse()
    {
        var detector = new SecretDetector();
        Assert.IsFalse(detector.ContainsSecrets(null!));
    }

    [TestMethod]
    public void ContainsSecrets_EmptyText_ReturnsFalse()
    {
        var detector = new SecretDetector();
        Assert.IsFalse(detector.ContainsSecrets(""));
    }

    [TestMethod]
    public void RedactSecrets_NullText_ReturnsEmptyString()
    {
        var detector = new SecretDetector();
        Assert.AreEqual("", detector.RedactSecrets(null!));
    }

    [TestMethod]
    public void RedactSecrets_EmptyText_ReturnsEmptyString()
    {
        var detector = new SecretDetector();
        Assert.AreEqual("", detector.RedactSecrets(""));
    }

    [TestMethod]
    public void UrlContainsCredentials_NullUrl_ReturnsFalse()
    {
        var detector = new SecretDetector();
        Assert.IsFalse(detector.UrlContainsCredentials(null!));
    }

    [TestMethod]
    public void UrlContainsCredentials_EmptyUrl_ReturnsFalse()
    {
        var detector = new SecretDetector();
        Assert.IsFalse(detector.UrlContainsCredentials(""));
    }

    // --- KnownPrefixPattern tests ---

    [TestMethod]
    public void Detects_OpenAiApiKey()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("key=sk-abc123def456ghi789jkl012"));
    }

    [TestMethod]
    public void Detects_GithubPAT()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("token=ghp_ABCDEFGHIJKLMNOPQRSTUVWXYZ"));
    }

    [TestMethod]
    public void Detects_GithubFineGrainedPAT()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("github_pat_ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefgh"));
    }

    [TestMethod]
    public void Detects_SlackToken()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("xoxb-123456789012-1234567890123-AbCdEfGhIjKlMnOpQrStUv"));
    }

    [TestMethod]
    public void Detects_AwsAccessKey()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("AKIAIOSFODNN7EXAMPLE"));
    }

    [TestMethod]
    public void Detects_StripeLiveKey()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("sk_live_ABCDEFGHIJKLMNOPQRSTUV"));
    }

    [TestMethod]
    public void Detects_StripeTestKey()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("sk_test_ABCDEFGHIJKLMNOPQRSTUV"));
    }

    [TestMethod]
    public void Detects_SendGridKey()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("SG.abcdefghijklmnopqrstuv"));
    }

    [TestMethod]
    public void Detects_HuggingFaceKey()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("hf_ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefgh"));
    }

    [TestMethod]
    public void Detects_AIZaGoogleKey()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("AIzaSyA1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6Q"));
    }

    [TestMethod]
    public void Redacts_OpenAiApiKey()
    {
        var detector = new SecretDetector();
        var result = detector.RedactSecrets("my key is sk-abc123def456ghi789jkl012 ok");
        Assert.IsFalse(result.Contains("sk-abc123"));
        Assert.IsTrue(result.Contains("[REDACTED]"));
    }

    [TestMethod]
    public void Redacts_AwsKey_FullRedaction()
    {
        var detector = new SecretDetector();
        var result = detector.RedactSecrets("AKIAIOSFODNN7EXAMPLE");
        Assert.AreEqual("[REDACTED]", result);
    }

    // --- AuthorizationHeaderPattern tests ---

    [TestMethod]
    public void Detects_AuthorizationHeader()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("Authorization: Bearer abc123token456"));
    }

    [TestMethod]
    public void Redacts_AuthorizationHeader()
    {
        var detector = new SecretDetector();
        var result = detector.RedactSecrets("Authorization: Bearer abc123token456");
        Assert.AreEqual("Authorization: Bearer [REDACTED]", result);
    }

    // --- JsonFieldPattern tests ---

    [TestMethod]
    public void Detects_JsonApiKey()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("\"api_key\": \"sk-abc123def456ghi789jkl012\""));
    }

    [TestMethod]
    public void Detects_JsonToken()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("\"token\": \"ghp_ABCDEFGHIJKLMNOPQRSTUVWXYZ\""));
    }

    [TestMethod]
    public void Detects_JsonPassword()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("\"password\": \"super_secret_123\""));
    }

    [TestMethod]
    public void Redacts_JsonApiKey()
    {
        var detector = new SecretDetector();
        var result = detector.RedactSecrets("\"api_key\": \"sk-abc123def456ghi789jkl012\"");
        Assert.AreEqual("\"api_key\": \"[REDACTED]\"", result);
    }

    // --- PrivateKeyPattern tests ---

    [TestMethod]
    public void Detects_PrivateKey()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("-----BEGIN RSA PRIVATE KEY-----\nMIIEpAIBAAKCAQEA\n-----END RSA PRIVATE KEY-----"));
    }

    [TestMethod]
    public void Redacts_PrivateKey()
    {
        var detector = new SecretDetector();
        var result = detector.RedactSecrets("-----BEGIN RSA PRIVATE KEY-----\nMIIEpAIBAAKCAQEA\n-----END RSA PRIVATE KEY-----");
        Assert.AreEqual("[REDACTED PRIVATE KEY]", result);
    }

    // --- DbConnStrPattern tests ---

    [TestMethod]
    public void Detects_PostgresConnStr()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("postgres://user:password123@localhost:5432/db"));
    }

    [TestMethod]
    public void Detects_MysqlConnStr()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("mysql://admin:secretpass@db.example.com/mydb"));
    }

    [TestMethod]
    public void Detects_MongodbConnStr()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("mongodb://root:pass123@mongo.local:27017/admin"));
    }

    [TestMethod]
    public void Redacts_PostgresConnStr()
    {
        var detector = new SecretDetector();
        var result = detector.RedactSecrets("postgres://user:password123@localhost:5432/db");
        Assert.AreEqual("postgres://user:[REDACTED]@localhost:5432/db", result);
    }

    // --- UrlCredentialPattern tests ---

    [TestMethod]
    public void Detects_UrlWithCredentials()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.ContainsSecrets("https://user:password123@example.com/path"));
    }

    [TestMethod]
    public void UrlContainsCredentials_Detects_UrlWithCredentials()
    {
        var detector = new SecretDetector();
        Assert.IsTrue(detector.UrlContainsCredentials("https://user:password123@example.com/path"));
    }

    [TestMethod]
    public void UrlContainsCredentials_NoCredentials_ReturnsFalse()
    {
        var detector = new SecretDetector();
        Assert.IsFalse(detector.UrlContainsCredentials("https://example.com/path"));
    }

    // --- Pattern injection (dependency injection test) ---

    [TestMethod]
    public void ContainsSecrets_WithEmptyPatternList_ReturnsFalse()
    {
        var detector = new SecretDetector(Array.Empty<ISecretPattern>());
        Assert.IsFalse(detector.ContainsSecrets("sk-abc123def456ghi789jkl012"));
    }

    [TestMethod]
    public void RedactSecrets_WithEmptyPatternList_ReturnsOriginal()
    {
        var detector = new SecretDetector(Array.Empty<ISecretPattern>());
        var original = "sk-abc123def456ghi789jkl012";
        Assert.AreEqual(original, detector.RedactSecrets(original));
    }

    // --- SecretPatternLibrary tests ---

    [TestMethod]
    public void PatternLibrary_ReturnsSixPatterns()
    {
        var patterns = SecretPatternLibrary.All;
        Assert.AreEqual(6, patterns.Count);
    }

    [TestMethod]
    public void PatternLibrary_ContainsKnownPrefixPattern()
    {
        var patterns = SecretPatternLibrary.All;
        Assert.IsTrue(patterns.Any(p => p is KnownPrefixPattern));
    }

    [TestMethod]
    public void PatternLibrary_ContainsAuthorizationHeaderPattern()
    {
        var patterns = SecretPatternLibrary.All;
        Assert.IsTrue(patterns.Any(p => p is AuthorizationHeaderPattern));
    }

    [TestMethod]
    public void PatternLibrary_ContainsJsonFieldPattern()
    {
        var patterns = SecretPatternLibrary.All;
        Assert.IsTrue(patterns.Any(p => p is JsonFieldPattern));
    }

    [TestMethod]
    public void PatternLibrary_ContainsPrivateKeyPattern()
    {
        var patterns = SecretPatternLibrary.All;
        Assert.IsTrue(patterns.Any(p => p is PrivateKeyPattern));
    }

    [TestMethod]
    public void PatternLibrary_ContainsDbConnStrPattern()
    {
        var patterns = SecretPatternLibrary.All;
        Assert.IsTrue(patterns.Any(p => p is DbConnStrPattern));
    }

    [TestMethod]
    public void PatternLibrary_ContainsUrlCredentialPattern()
    {
        var patterns = SecretPatternLibrary.All;
        Assert.IsTrue(patterns.Any(p => p is UrlCredentialPattern));
    }

    // --- Static facade (SecretScanner) backward compat ---

    [TestMethod]
    public void StaticFacade_ContainsSecrets_DetectsApiKey()
    {
        Assert.IsTrue(SecretScanner.ContainsSecrets("sk-abc123def456ghi789jkl012"));
    }

    [TestMethod]
    public void StaticFacade_RedactSecrets_RedactsApiKey()
    {
        var result = SecretScanner.RedactSecrets("key=sk-abc123def456ghi789jkl012");
        Assert.IsFalse(result.Contains("sk-abc123"));
    }

    [TestMethod]
    public void StaticFacade_UrlContainsCredentials_DetectsUrlCreds()
    {
        Assert.IsTrue(SecretScanner.UrlContainsCredentials("https://user:pass@example.com"));
    }

    // --- No false positives ---

    [TestMethod]
    public void DoesNotDetect_NormalText()
    {
        var detector = new SecretDetector();
        Assert.IsFalse(detector.ContainsSecrets("Hello world, this is a normal sentence."));
    }

    [TestMethod]
    public void DoesNotDetect_GitCommitHash()
    {
        var detector = new SecretDetector();
        Assert.IsFalse(detector.ContainsSecrets("abc123def456"));
    }

    [TestMethod]
    public void DoesNotDetect_UrlWithoutCredentials()
    {
        var detector = new SecretDetector();
        Assert.IsFalse(detector.ContainsSecrets("https://example.com/path/to/resource"));
    }

    [TestMethod]
    public void DoesNotDetect_ShortSkPrefix()
    {
        var detector = new SecretDetector();
        // sk- with fewer than 10 chars after should not match the prefix pattern
        Assert.IsFalse(detector.ContainsSecrets("sk-short"));
    }
}
