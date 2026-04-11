namespace Hermes.Agent.Dreamer;

/// <summary>Observable state for UI (Dashboard) — updated from the Dreamer background loop.</summary>
public sealed class DreamerStatus
{
    private readonly object _lock = new();
    private string _phase = "idle";
    private int _walkCount;
    private string _lastWalkSummary = "";
    private string _lastPostcardPreview = "";
    private double _topSignalScore;
    private string _topSignalSlug = "";

    /// <summary>
    /// Produces an immutable snapshot of the current Dreamer status.
    /// </summary>
    /// <returns>A <see cref="DreamerStatusSnapshot"/> containing the current phase, walk count, last walk summary, last postcard preview, top signal score, and top signal slug.</returns>
    public DreamerStatusSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new DreamerStatusSnapshot(
                _phase,
                _walkCount,
                _lastWalkSummary,
                _lastPostcardPreview,
                _topSignalScore,
                _topSignalSlug);
        }
    }

    /// <summary>
    /// Sets the current phase of the Dreamer background loop.
    /// </summary>
    /// <param name="phase">The new phase name (for example "idle" or "walking").</param>
    public void SetPhase(string phase)
    {
        lock (_lock) { _phase = phase; }
    }

    /// <summary>
    /// Atomically updates the status after a completed walk: sets the phase to "idle" and records the walk summary, walk number, and top-signal metadata.
    /// </summary>
    /// <param name="walkPreview">A short summary or preview of the walk that just completed.</param>
    /// <param name="walkNumber">The sequential number of the completed walk.</param>
    /// <param name="topScore">The top signal score found during the walk.</param>
    /// <param name="topSlug">The identifier (slug) of the top signal found during the walk.</param>
    public void AfterWalk(string walkPreview, int walkNumber, double topScore, string topSlug)
    {
        lock (_lock)
        {
            _phase = "idle";
            _walkCount = walkNumber;
            _lastWalkSummary = walkPreview;
            _topSignalScore = topScore;
            _topSignalSlug = topSlug;
        }
    }

    /// <summary>
    /// Updates the stored postcard preview text in the status object in a thread-safe manner.
    /// </summary>
    /// <param name="text">The text to set as the last postcard preview.</param>
    public void SetPostcardPreview(string text)
    {
        lock (_lock) { _lastPostcardPreview = text; }
    }
}

public readonly record struct DreamerStatusSnapshot(
    string Phase,
    int WalkCount,
    string LastWalkSummary,
    string LastPostcardPreview,
    double TopSignalScore,
    string TopSignalSlug);
