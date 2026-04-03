using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hermes.Agent.Core;
using Hermes.Agent.LLM;
using Hermes.Agent.Permissions;

namespace HermesDesktop.Services;

internal sealed class HermesChatService : IDisposable
{
    private readonly OpenAiClient _chatClient;
    private string? _sessionId;
    private bool _disposed;

    public HermesChatService()
    {
        var configPath = Environment.GetEnvironmentVariable("HERMES_CONFIG") 
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "hermes", "config.yaml");
        
        var config = new LlmConfig
        {
            Provider = "openai",
            Model = "qwen3.5",
            BaseUrl = "http://localhost:11434/v1",
            ApiKey = ""
        };
        
        _chatClient = new OpenAiClient(config, new HttpClient());
    }

    public string? CurrentSessionId => _sessionId;
    public PermissionMode CurrentPermissionMode { get; private set; } = PermissionMode.Default;

    public async Task<(bool IsHealthy, string Detail)> CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            var messages = new[] { new Message { Role = "user", Content = "Respond with only: OK" } };
            var response = await _chatClient.CompleteAsync(messages, cancellationToken);
            
            return !string.IsNullOrEmpty(response)
                ? (true, $"OpenAiClient · Connected")
                : (false, "Empty response from LLM");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<HermesChatReply> SendAsync(string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_sessionId))
        {
            _sessionId = Guid.NewGuid().ToString("N")[..8];
        }

        var userMessage = new Message { Role = "user", Content = message };
        var messages = new[] { userMessage };

        var response = await _chatClient.CompleteAsync(messages, cancellationToken);
        
        if (!string.IsNullOrEmpty(response))
        {
            return new HermesChatReply(response, _sessionId);
        }

        throw new InvalidOperationException("No response from LLM");
    }

    public void SetPermissionMode(PermissionMode mode)
    {
        CurrentPermissionMode = mode;
    }

    public void ResetConversation()
    {
        _sessionId = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }

    internal sealed record HermesChatReply(string Response, string SessionId);
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
