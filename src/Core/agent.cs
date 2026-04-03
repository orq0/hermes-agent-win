namespace Hermes.Agent.Core;

using Hermes.Agent.LLM;
using Microsoft.Extensions.Logging;

public sealed class Agent : IAgent
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<Agent> _logger;
    private readonly Dictionary<string, ITool> _tools = new();
    
    public Agent(IChatClient chatClient, ILogger<Agent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }
    
    public void RegisterTool(ITool tool)
    {
        _tools[tool.Name] = tool;
        _logger.LogInformation("Registered tool: {ToolName}", tool.Name);
    }
    
    public async Task<string> ChatAsync(string message, Session session, CancellationToken ct)
    {
        session.AddMessage(new Message { Role = "user", Content = message });
        _logger.LogInformation("Processing message for session {SessionId}", session.Id);
        
        var response = await _chatClient.CompleteAsync(session.Messages, ct);
        
        session.AddMessage(new Message { Role = "assistant", Content = response });
        return response;
    }
}
