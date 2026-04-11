namespace Hermes.Agent.Dreamer;

using Hermes.Agent.Core;
using Hermes.Agent.LLM;
using Microsoft.Extensions.Logging;

/// <summary>One free-association walk: mode selection, prompt assembly, LLM call, journaling.</summary>
public sealed class DreamWalk
{
    private readonly DreamerRoom _room;
    private readonly IChatClient _walkClient;
    private readonly ILogger<DreamWalk> _logger;
    private readonly Random _rng = new();

    public DreamWalk(DreamerRoom room, IChatClient walkClient, ILogger<DreamWalk> logger)
    {
        _room = room;
        _walkClient = walkClient;
        _logger = logger;
    }

    public async Task<string> RunAsync(
        DreamerConfig config,
        string researchContext,
        string? priorWalkExcerpt,
        CancellationToken ct)
    {
        var mode = PickMode();
        var soul = File.Exists(_room.SoulPath) ? await File.ReadAllTextAsync(_room.SoulPath, ct) : "";
        var fasc = File.Exists(_room.FascinationsPath) ? await File.ReadAllTextAsync(_room.FascinationsPath, ct) : "";

        var prompt = $"""
            ## Dreamer walk mode: {mode}
            {soul}

            ## Fascinations
            {fasc[..Math.Min(4000, fasc.Length)]}

            ## Research context (transcripts / inbox excerpts)
            {researchContext[..Math.Min(12000, researchContext.Length)]}

            ## Prior walk (for continuity)
            {(priorWalkExcerpt ?? "(none)")[..Math.Min(2000, (priorWalkExcerpt ?? "(none)").Length)]}

            Write this walk as a dated journal entry. End with `[BUILD: slug]` only if you have a concrete sandbox project idea; otherwise omit that line.
            """;

        var text = await _walkClient.CompleteAsync(
            new[] { new Message { Role = "user", Content = prompt } },
            ct);

        var path = _room.NewWalkPath();
        await File.WriteAllTextAsync(path,
            $"# Walk {DateTime.UtcNow:O}\n\n## Mode: {mode}\n\n{text}\n",
            ct);

        _logger.LogInformation("Dreamer walk completed → {Path}", path);
        return text;
    }

    private string PickMode()
    {
        var r = _rng.NextDouble();
        if (r < 0.40) return "drift";
        if (r < 0.70) return "continue";
        if (r < 0.90) return "tangent";
        return "tend";
    }
}
