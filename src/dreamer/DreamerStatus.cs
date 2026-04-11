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

    public void SetPhase(string phase)
    {
        lock (_lock) { _phase = phase; }
    }

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
