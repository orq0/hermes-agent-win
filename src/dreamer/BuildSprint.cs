namespace Hermes.Agent.Dreamer;

using Microsoft.Extensions.Logging;

/// <summary>Autonomous build trigger — sandboxed under dreamer/projects/{slug}/.</summary>
public sealed class BuildSprint
{
    private readonly DreamerRoom _room;
    private readonly ILogger<BuildSprint> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="BuildSprint"/> that scaffolds sandbox workspaces under the room's ProjectsDir.
    /// </summary>
    /// <param name="room">Provides the base projects directory used to create per-project sandboxes.</param>
    /// <param name="logger">Logger used to record scaffold completion and related informational messages.</param>
    public BuildSprint(DreamerRoom room, ILogger<BuildSprint> logger)
    {
        _room = room;
        _logger = logger;
    }

    /// <summary>
    /// Creates project workspace and seeds documentation. Full <c>Agent</c> tool execution is deferred;
    /// autonomy controls how much scaffolding is written.
    /// <summary>
    /// Scaffolds a sandbox project workspace under the room's projects directory and seeds initial documentation files.
    /// </summary>
    /// <param name="slug">Project slug used to name the workspace; the value is sanitized and validated. An invalid or dangerous slug causes an <see cref="ArgumentException"/>.</param>
    /// <param name="walkExcerpt">Seed text placed in the README's "Seed intent" section; the value is truncated to at most 8000 characters.</param>
    /// <param name="autonomy">Autonomy mode string included in the README. If not equal to "ideas" (case-insensitive), a SPRINT.md checklist is also created.</param>
    /// <param name="ct">Cancellation token for the directory and file write operations.</param>
    /// <returns>A task that completes after the workspace directory has been created and the README (and conditionally SPRINT.md) have been written.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="slug"/> is null, empty, or considered dangerous after sanitization.</exception>
    public async Task RunAsync(string slug, string walkExcerpt, string autonomy, CancellationToken ct)
    {
        var sanitized = SanitizeSlug(slug);
        if (string.IsNullOrEmpty(sanitized))
            throw new ArgumentException("Invalid or dangerous project slug", nameof(slug));

        var dir = Path.Combine(_room.ProjectsDir, sanitized);
        Directory.CreateDirectory(dir);

        var readme = $"""
            # Dreamer build: {slug}

            **Autonomy mode:** {autonomy}

            This directory is a **sandbox**. Nothing here is merged into the main Hermes tree until you promote it manually.

            ## Seed intent (from walk)
            {walkExcerpt[..Math.Min(8000, walkExcerpt.Length)]}

            ## Next steps
            - `ideas`: keep as notes only.
            - `drafts`: expand SPRINT.md with a checklist (no code execution).
            - `full`: reserved for future automated agent runs with tools pinned to this directory.
            """;

        await File.WriteAllTextAsync(Path.Combine(dir, "README.md"), readme, ct);

        if (!string.Equals(autonomy, "ideas", StringComparison.OrdinalIgnoreCase))
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "SPRINT.md"),
                "## Checklist\n\n- [ ] Clarify scope\n- [ ] Prototype\n- [ ] Tests\n",
                ct);
        }

        _logger.LogInformation("Dreamer build sprint scaffolded at {Dir}", dir);
    }

    /// <summary>
    /// Produce a filesystem-safe project slug suitable for use as a directory name.
    /// </summary>
    /// <param name="slug">The input slug to sanitize.</param>
    /// <returns>
    /// A sanitized slug safe for use as a filename/directory: removes path traversal sequences and path separators, strips characters invalid for filenames, and trims whitespace. Returns an empty string if the input is null/whitespace or if the result would be a rooted or otherwise unsafe path.
    /// </returns>
    private static string SanitizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return "";

        // Remove dangerous path sequences
        var result = slug.Replace("..", "")
                         .Replace("/", "-")
                         .Replace("\\", "-");

        // Remove invalid filename chars
        foreach (var c in Path.GetInvalidFileNameChars())
            result = result.Replace(c.ToString(), "");

        // Ensure not rooted
        if (Path.IsPathRooted(result))
            return "";

        return result.Trim();
    }
}