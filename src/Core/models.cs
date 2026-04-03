namespace Hermes.Agent.Core;

public sealed class Message
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? ToolCallId { get; init; }
    public string? ToolName { get; init; }
}

public sealed class Session
{
    public required string Id { get; init; }
    public string? UserId { get; init; }
    public string? Platform { get; init; }
    public List<Message> Messages { get; init; } = new();
    public Dictionary<string, object> State { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    public void AddMessage(Message message)
    {
        Messages.Add(message);
        LastActivityAt = DateTime.UtcNow;
    }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
}

public sealed class ToolResult
{
    public bool Success { get; init; }
    public required string Content { get; init; }
    public Exception? Error { get; init; }
    
    public static ToolResult Ok(string content) => new() { Success = true, Content = content };
    public static ToolResult Fail(string error, Exception? ex = null) => new() { Success = false, Content = error, Error = ex };
}

public interface ITool
{
    string Name { get; }
    string Description { get; }
    Type ParametersType { get; }
    Task<ToolResult> ExecuteAsync(object parameters, CancellationToken ct);
}

public interface IAgent
{
    Task<string> ChatAsync(string message, Session session, CancellationToken ct);
    void RegisterTool(ITool tool);
}
