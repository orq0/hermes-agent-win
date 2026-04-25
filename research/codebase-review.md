# Hermes Desktop -- Codebase Review: Security, Malware, Spyware, and Quality

**Date**: 2026-04-25
**Scope**: Full codebase scan (130+ C# files, ~15K lines)

---

## Executive Summary

**No malware or spyware detected.** The codebase does not contain intentional data exfiltration, backdoors, or malicious behavior. All analytics are local-only. However, several **structural security weaknesses** and **quality issues** create risk of accidental data exposure and command injection.

---

## 1. SECURITY FINDINGS

### CRITICAL

| # | Issue | File | Line(s) | Risk |
|---|-------|------|---------|------|
| C1 | **Docker shell injection** -- command embedded in `bash -c` with only quote escaping; shell metacharacters (`;`, `|`, `$()`, backticks) are unescaped | `src/execution/DockerBackend.cs` | 58 | Remote code execution if command comes from untrusted source |
| C2 | **Config-driven command injection** -- `AuthTokenCommand` from config.yaml passed directly to `cmd.exe /c` | `src/LLM/openaiclient.cs` | 394-422 | Arbitrary command execution via config manipulation |
| C3 | **TerminalTool has NO security analysis** -- `BashTool` has 22 chained validators; `TerminalTool` passes commands directly to `cmd.exe /c` with no validation | `src/Tools/terminaltool.cs` | 25 | Inconsistent security coverage between two shell-exec tools |
| C4 | **HttpClient leak in ChatClientFactory** -- `CreateHttpClientForProvider()` creates new `HttpClient` on every provider switch; old clients are never disposed | `src/LLM/ChatClientFactory.cs` | 94-103 | Socket exhaustion over time |

### HIGH

| # | Issue | File | Line(s) | Risk |
|---|-------|------|---------|------|
| H1 | **No PII filtering in Dreamer** -- transcript excerpts + inbox content sent to LLM without sanitizing API keys, emails, phone numbers | `src/dreamer/DreamerService.cs` | 283-391, `src/dreamer/DreamWalk.cs` | 43-57 | Sensitive data exposure in LLM prompts |
| H2 | **API key as URL query param** -- Google API key sent in URL (logged by proxies/servers) | `src/Tools/websearchtool.cs` | 101 | Credential leakage via URL logs |
| H3 | **Telegram bot token in URL** -- token embedded in URL path (`/bot{token}/`) | `src/gateway/platforms/TelegramAdapter.cs` | 41 | Credential leakage via URL logs |
| H4 | **Unbounded TranscriptStore cache** -- `_cache` dictionary grows without eviction | `src/transcript/transcriptstore.cs` | 17 | Memory leak in long-running sessions |
| H5 | **Unbounded ActivityLog** -- `Agent.ActivityLog` list never trimmed | `src/Core/agent.cs` | 62 | Memory leak |
| H6 | **Fire-and-forget tasks with no error tracking** -- `Task.Run` without cancellation or logging | `src/Core/agent.cs` | 454, 774 | Silent failures, unobserved exceptions |
| H7 | **DMs authorized by default** -- when no `AllowedUsers` config, all DMs get Tier 5 (full access) | `src/gateway/GatewayService.cs` | 269-274 | Unauthorized access if bot is publicly discoverable |

### MEDIUM

| # | Issue | File | Line(s) | Risk |
|---|-------|------|---------|------|
| M1 | **Dreamer digest sends personal data to Discord** -- walk text (transcript excerpts) posted to configured Discord channel | `src/dreamer/DreamerService.cs` | 250-252 | Data exposure if channel misconfigured |
| M2 | **Build sprint defaults to OpenAI cloud** -- walk uses local model by default, but build uses OpenAI | `src/dreamer/DreamerConfig.cs` | 16-18 | Privacy inconsistency |
| M3 | **EchoDetector LLM call has no timeout** -- blocks entire walk cycle if LLM hangs | `src/dreamer/EchoDetector.cs` | 41-43 | Denial of service in dreamer cycle |
| M4 | **RSS fetch has no retry logic** -- silent failure on temporary outages | `src/dreamer/RssFetcher.cs` | 54-86 | Data loss |
| M5 | **No path validation in file tools** -- write/edit/read tools accept raw file paths without sandboxing | `src/Tools/writefiletool.cs`, `editfiletool.cs`, `readfiletool.cs` | entire files | Path traversal / sensitive file access |
| M6 | **Empty catch blocks silently swallow errors** -- 5 files in MCP layer | `src/mcp/` | various | Hard to debug, masked failures |
| M7 | **No config validation** -- `LlmConfig` is a plain class with no validation | `src/Program.cs` | 84-90 | Silent misconfiguration |
| M8 | **Session not thread-safe** -- `Session.Messages` accessed from gateway + agent concurrently | `src/Core/models.cs` | 21 | Race conditions / data corruption |

### LOW

| # | Issue | File | Line(s) | Risk |
|---|-------|------|---------|------|
| L1 | **WikiSearchIndex never disposed** -- WikiManager doesn't implement IDisposable | `src/wiki/WikiSearchIndex.cs` | 24 | SQLite file handle leak |
| L2 | **SemaphoreSlim not disposed** in DreamerRoom | `src/dreamer/DreamerRoom.cs` | 11 | Minor resource leak |
| L3 | **Incomplete FTS5 query sanitization** -- only 3 chars sanitized | `src/wiki/WikiSearchIndex.cs` | 187-193 | Potential FTS5 injection |
| L4 | **Exception messages stored in analytics JSON** | `src/analytics/InsightsService.cs` | 114-119 | Info leak via analytics file |
| L5 | **No `ConfigureAwait(false)`** anywhere in library code | Entire codebase | -- | Potential deadlocks in STA contexts |

---

## 2. MALWARE / SPYWARE ANALYSIS

### No malicious code detected:

- **No hidden data exfiltration**: All external calls are to configured services (LLM providers, Telegram, Discord, search engines)
- **No telemetry to third parties**: `InsightsService` writes only to local `insights.json`
- **No hardcoded credentials**: Only a placeholder `"no-key-required"` in example config
- **No backdoors or hidden functionality**: All code paths are traceable to user-facing features
- **No reverse shell or covert channels**: Network calls are to known APIs with proper HTTP semantics

### Privacy concerns (not malware, but worth noting):

1. **Dreamer reads all transcript data** and sends excerpts to LLM -- this includes any sensitive info the user pasted in conversations (passwords, keys, personal data)
2. **Dreamer digest to Discord** sends conversation-derived content to an external channel
3. **Build sprint defaults to OpenAI** -- project code could be sent to cloud LLM without explicit opt-in

---

## 3. CODE QUALITY FINDINGS

### Major Issues

| # | Issue | File | Impact |
|---|-------|------|--------|
| Q1 | **Agent.cs is a God class** -- 986 lines, handles tool execution, permissions, activity logging, secret scanning, tool schema building | `src/Core/agent.cs` | Hard to maintain/test |
| Q2 | **70% code duplication** between `ChatAsync` and `StreamChatAsync` | `src/Core/agent.cs` | Bug propagation risk |
| Q3 | **Raw `new HttpClient()` in Program.cs** -- socket exhaustion risk | `src/Program.cs` | 92 | Production reliability |
| Q4 | **No tests for critical paths** -- SecretScanner, GatewayService, LLM clients, MCP, execution backends untested | Multiple | Regression risk |
| Q5 | **Inconsistent naming** -- mixed PascalCase/lowercase filenames | Throughout | Professionalism |
| Q6 | **Reflected JSON Schema only handles basic types** -- enums, nested objects fall back to `"string"` | `src/Core/agent.cs` | 887-932 | Tool parameter correctness |

### Good Practices Found

- **CredentialPool** with rotation on auth failures -- well-designed
- **SecretScanner** with 16+ regex patterns for secret detection -- comprehensive
- **ShellSecurityAnalyzer** with 22 chained validators in BashTool -- good defense-in-depth
- **Atomic transcript writes** with `WriteThrough` flag -- crash-proof persistence
- **Plugin system** with lifecycle hooks -- extensible architecture
- **Local-only analytics** -- privacy-respecting by design
- **Dreamer error isolation** -- per-operation try/catch prevents cascade failures

---

## 4. RECOMMENDATIONS

### Priority 1: Security Fixes

1. **Add `IAsyncDisposable` to ChatClientFactory** -- dispose old HttpClient before creating new one (C4)
2. **Add ShellSecurityAnalyzer to TerminalTool** -- mirror BashTool's 22 validators (C3)
3. **Sanitize DockerBackend command** -- use `ProcessStartInfo.ArgumentList` instead of string interpolation (C1)
4. **Move Google API key to header** -- use `Authorization` or custom header instead of URL param (H1)
5. **Add PII redaction layer** before LLM context assembly in DreamerService (H1)

### Priority 2: Resource Management

6. **Add cache eviction to TranscriptStore** -- TTL or max-size with LRU (H4)
7. **Cap ActivityLog size** -- sliding window of last 1000 entries (H5)
8. **Wire up `TaskScheduler.UnobservedTaskException`** -- log unobserved exceptions (H6)

### Priority 3: Quality Improvements

9. **Extract shared tool execution logic** from Agent.ChatAsync/StreamChatAsync (Q2)
10. **Split Agent.cs** -- extract permission checking, activity logging into separate classes (Q1)
11. **Add config validation** -- `DataAnnotations` or source generators for LlmConfig (M7)
12. **Add tests for SecretScanner** -- critical security component with 0 coverage (Q4)
13. **Standardize file naming** to PascalCase (Q5)

### Priority 4: Privacy Hardening

14. **Require explicit DM authorization** by default (H7)
15. **Make build LLM default configurable** -- don't default to OpenAI cloud (M2)
16. **Add timeout to EchoDetector LLM calls** (M3)
17. **Add RSS retry with exponential backoff** (M4)

---

## 5. FILES REQUIRING IMMEDIATE ATTENTION

| Priority | File | Reason |
|----------|------|--------|
| Critical | `src/execution/DockerBackend.cs` | Shell injection vulnerability |
| Critical | `src/LLM/openaiclient.cs` | Config-driven command injection |
| Critical | `src/Tools/terminaltool.cs` | Missing security analysis |
| Critical | `src/LLM/ChatClientFactory.cs` | HttpClient leak |
| High | `src/dreamer/DreamerService.cs` | PII in LLM context |
| High | `src/Tools/websearchtool.cs` | API key in URL |
| High | `src/transcript/transcriptstore.cs` | Memory leak |
| High | `src/Core/agent.cs` | God class + code duplication |


-----------------------
Critical to running Hermes safely:

  1. SecretScanner — This is the module that scans all tool output for secrets (API keys, tokens, credentials) before
  they reach the LLM. Without it, secrets get fed into LLM prompts and could be exfiltrated to cloud providers. This is
  the most critical for safety.
  2. IExecutionBackend — The DockerBackend shell injection vulnerability (C1 in the review) is the highest-risk finding.
   If the command comes from an untrusted source (e.g., the LLM constructs it via tool output), shell metacharacters
  bypass all escaping. This is a remote code execution vector.
  3. TerminalTool missing ShellSecurityAnalyzer — C3 in the review. TerminalTool executes commands with zero validation
  while BashTool has 22 validators. This is an inconsistent security gap — the agent could bypass BashTool's protections
   by calling terminal instead.

## RECOMMENDATION

 Current state:
  - SecretScanner is a static class with 6 hardcoded regex patterns, 3 public methods (ContainsSecrets, RedactSecrets,
  UrlContainsCredentials), and one private helper (MaskToken)
  - It's called at 7 call sites across agent.cs (3x), webfetchtool.cs (1x), skillmanager.cs (3x)
  - Zero tests exist
  - The regex patterns are embedded directly in the class — no way to test them independently or swap them

  Deepening design:

  Seam: ISecretDetector
  interface ISecretDetector
  {
      bool ContainsSecrets(string? text);
      string RedactSecrets(string? text);
      bool UrlContainsCredentials(string? url);
  }

  Deep module: SecretPatternLibrary — owns the regex patterns as composable units. Each pattern is a named, testable
  unit with its own interface:
  interface ISecretPattern
  {
      string Name { get; }
      bool Matches(string text);
      string ApplyRedaction(string text);
  }

  SecretDetector composes SecretPatternLibrary — the patterns are injected, not hardcoded. This means:
  - Tests can verify each pattern independently
  - Tests can inject a fake detector for integration tests
  - Production uses the full pattern library
  - Future patterns can be added without editing SecretDetector

  Benefits:
  - Locality: Each regex pattern is isolated in its own testable unit. A bug in one pattern doesn't require running all
  6 patterns to reproduce.
  - Leverage: Callers still get a simple 3-method interface. The complexity of pattern management is behind the seam.
  - Test surface: ISecretDetector is testable via injection. Each ISecretPattern is independently testable. Integration
  tests mock ISecretDetector.

  What changes:
  1. src/security/ISecretDetector.cs — new interface
  2. src/security/SecretPatternLibrary.cs — new module with pattern definitions
  3. src/security/SecretPattern.cs — new ISecretPattern interface + base implementation
  4. src/security/SecretDetector.cs — refactored from static to non-static, composes patterns
  5. src/security/SecretScanner.cs — becomes a thin static facade delegating to ISecretDetector (backward compat)
  6. src/security/SecretPatternLibrary.cs — each pattern is a separate, testable unit

  What stays the same:
  - SecretScanner.ContainsSecrets() and SecretScanner.RedactSecrets() still work as static methods (backward compat)
  - All 7 call sites continue to work without changes
  - The regex patterns themselves don't change  

## APPLIED FIXES
  
  Architecture improvements:

  - Locality: Each regex pattern is independently testable. A bug in one pattern doesn't require running all 6 to
  reproduce.
  - Leverage: Callers still get a simple 3-method static interface. Complexity is behind the seam.
  - Test surface: ISecretDetector is testable via injection. Each ISecretPattern is independently testable. Integration
  tests can mock ISecretDetector.
  - Open/closed: Adding a new pattern requires zero changes to SecretDetector or SecretScanner — just add a new
  ISecretPattern implementation and register it.

  