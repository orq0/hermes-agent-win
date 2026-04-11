namespace Hermes.Agent.Dreamer;

using System.Net.Http;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

/// <summary>Lightweight RSS fetch — writes markdown summaries into inbox-rss/.</summary>
public sealed class RssFetcher
{
    private readonly HttpClient _http;
    private readonly DreamerRoom _room;
    private readonly ILogger<RssFetcher> _logger;
    private DateTime _lastRunUtc = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="RssFetcher"/> class with the HTTP client, target room, and logger dependencies.
    /// </summary>
    /// <param name="http">The <see cref="HttpClient"/> used to download feed content.</param>
    /// <param name="room">The <see cref="DreamerRoom"/> that provides the inbox RSS directory for output files.</param>
    /// <param name="logger">The logger used to record warnings for individual feed failures.</param>
    public RssFetcher(HttpClient http, DreamerRoom room, ILogger<RssFetcher> logger)
    {
        _http = http;
        _room = room;
        _logger = logger;
    }

    /// <summary>
    /// Fetches configured RSS/Atom feeds and writes per-feed markdown digests to the room inbox when due.
    /// </summary>
    /// <remarks>
    /// Execution is throttled to at most once every six hours; if <paramref name="feeds"/> is empty or the throttle interval has not elapsed, the method returns immediately.
    /// For each URL it attempts to download, parse (RSS or Atom) and write a markdown file containing up to 8 entries. Individual feed failures are logged as warnings and do not stop processing of other feeds. Cancellation is honored and will be rethrown.
    /// The internal last-run timestamp is updated only if at least one feed write succeeds.
    /// </remarks>
    /// <param name="feeds">A read-only list of feed URLs to fetch.</param>
    /// <param name="ct">A cancellation token that cancels network and file operations.</param>
    public async Task RunIfDueAsync(IReadOnlyList<string> feeds, CancellationToken ct)
    {
        if (feeds.Count == 0) return;
        if ((DateTime.UtcNow - _lastRunUtc).TotalHours < 6) return;

        bool anySuccess = false;
        foreach (var url in feeds)
        {
            try
            {
                var xml = await _http.GetStringAsync(new Uri(url), ct);
                var doc = XDocument.Parse(xml);
                XNamespace ns = "http://www.w3.org/2005/Atom";
                var items = doc.Descendants("item").Take(8).ToList();
                if (items.Count == 0)
                    items = doc.Descendants(ns + "entry").Take(8).ToList();

                var lines = new List<string> { $"# RSS digest: {url}", "" };
                foreach (var el in items)
                {
                    var title = el.Element("title")?.Value ?? el.Element(ns + "title")?.Value ?? "(untitled)";
                    var link = el.Element("link")?.Value ?? el.Element(ns + "link")?.Attribute("href")?.Value ?? "";
                    lines.Add($"- **{title}** — {link}");
                }

                var safe = string.Join("_", url.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                var path = Path.Combine(_room.InboxRssDir, $"rss-{safe[..Math.Min(48, safe.Length)]}-{DateTime.UtcNow:yyyyMMdd}.md");
                await File.WriteAllTextAsync(path, string.Join("\n", lines), ct);
                anySuccess = true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RSS fetch failed for {Url}", url);
            }
        }

        // Only update timestamp if at least one feed succeeded
        if (anySuccess)
        {
            _lastRunUtc = DateTime.UtcNow;
        }
    }
}