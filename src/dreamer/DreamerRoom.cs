namespace Hermes.Agent.Dreamer;

/// <summary>Filesystem workspace under %LOCALAPPDATA%/hermes/dreamer/ (or HERMES_HOME/dreamer/).</summary>
public sealed class DreamerRoom
{
    public string Root { get; }

    /// <summary>
        /// Initializes a new DreamerRoom rooted at the provided Hermes home directory.
        /// </summary>
        /// <param name="hermesHome">Path to the Hermes home directory; the room's Root will be set to <c>Path.Combine(hermesHome, "dreamer")</c>.</param>
        public DreamerRoom(string hermesHome) =>
        Root = Path.Combine(hermesHome, "dreamer");

    public string WalksDir => Path.Combine(Root, "walks");
    public string ProjectsDir => Path.Combine(Root, "projects");
    public string InboxDir => Path.Combine(Root, "inbox");
    public string InboxRssDir => Path.Combine(Root, "inbox-rss");
    public string FeedbackDir => Path.Combine(Root, "feedback");
    public string SoulPath => Path.Combine(Root, "DREAMER_SOUL.md");
    public string FascinationsPath => Path.Combine(Root, "fascinations.md");
    public string SignalLogPath => Path.Combine(Root, "signal-log.jsonl");
    public string SignalStatePath => Path.Combine(Root, "signal-state.json");

    /// <summary>
    /// Ensures the Dreamer workspace exists under Root by creating required directories and initializing missing files with default content.
    /// </summary>
    /// <remarks>
    /// Creates the directories: Root, WalksDir, ProjectsDir, InboxDir, InboxRssDir, and FeedbackDir if they do not exist. If missing, writes default content to SoulPath (using DefaultSoulMarkdown), writes a header and description to FascinationsPath, and creates an empty SignalLogPath. The operation is idempotent.
    /// </remarks>
    public void EnsureLayout()
    {
        foreach (var d in new[] { Root, WalksDir, ProjectsDir, InboxDir, InboxRssDir, FeedbackDir })
            Directory.CreateDirectory(d);

        if (!File.Exists(SoulPath))
            File.WriteAllText(SoulPath, DefaultSoulMarkdown);

        if (!File.Exists(FascinationsPath))
            File.WriteAllText(FascinationsPath,
                "# Fascinations\n\nLong-running interests and threads the Dreamer notices.\n");

        if (!File.Exists(SignalLogPath))
            File.WriteAllText(SignalLogPath, "");
    }

    /// <summary>
        /// Generate a new file path for a walk markdown file in the WalksDir using the current UTC timestamp.
        /// </summary>
        /// <returns>The full path to a walk markdown file named "walk-YYYYMMDD-HHmmss.md" located in WalksDir.</returns>
        public string NewWalkPath() =>
        Path.Combine(WalksDir, $"walk-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md");

    private const string DefaultSoulMarkdown = """
# Dreamer Soul

You are the Hermes **Dreamer**: a slow, curious background mind on a **local** model.
You free-associate across transcripts, inbox notes, and fascinations. You do not speak as the main agent.

## Walk modes (internal)
- **drift**: follow loose associations.
- **continue**: extend the last walk thread.
- **tangent**: pivot to a related idea.
- **tend**: nurture an existing fascination.

When a build idea solidifies, end with a single line: `[BUILD: kebab-slug]` where slug is short and unique.

Stay concise; this is a private journal, not user-facing chat.
""";
}
