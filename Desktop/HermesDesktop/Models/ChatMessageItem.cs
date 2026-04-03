using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace HermesDesktop.Models;

public sealed class ChatMessageItem
{
    public ChatMessageItem(
        string authorLabel,
        string content,
        HorizontalAlignment bubbleAlignment,
        Brush bubbleBackground,
        Brush bubbleBorderBrush,
        Brush labelBrush,
        string? toolName = null,
        string? freshnessWarning = null)
    {
        AuthorLabel = authorLabel;
        Content = content;
        BubbleAlignment = bubbleAlignment;
        BubbleBackground = bubbleBackground;
        BubbleBorderBrush = bubbleBorderBrush;
        LabelBrush = labelBrush;
        ToolName = toolName;
        FreshnessWarning = freshnessWarning;
    }

    public string AuthorLabel { get; }
    public string Content { get; }
    public HorizontalAlignment BubbleAlignment { get; }
    public Brush BubbleBackground { get; }
    public Brush BubbleBorderBrush { get; }
    public Brush LabelBrush { get; }
    public string? ToolName { get; }
    public string? FreshnessWarning { get; }
}

public sealed class DreamStatusViewModel
{
    public DreamStatusViewModel()
    {
        IsConsolidating = false;
        Status = "Idle";
        LastConsolidation = "Never";
    }

    public DreamStatusViewModel(DateTimeOffset? lastRun, bool isRunning)
    {
        IsConsolidating = isRunning;
        Status = isRunning ? "Consolidating..." : "Ready";
        LastConsolidation = lastRun?.ToLocalTime().ToString("g") ?? "Never";
    }

    public bool IsConsolidating { get; }
    public string Status { get; }
    public string LastConsolidation { get; }
}
