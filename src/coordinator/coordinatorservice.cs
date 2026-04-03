namespace Hermes.Agent.Coordinator;

using Hermes.Agent.LLM;
using Microsoft.Extensions.Logging;

/// <summary>
/// Coordinator Mode - Multi-worker orchestration.
/// Breaks complex tasks into subtasks, spawns workers, synthesizes results.
/// </summary>

public sealed class CoordinatorService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CoordinatorService> _logger;
    private readonly IChatClient _chatClient;
    
    public CoordinatorService(
        IServiceProvider services,
        ILogger<CoordinatorService> logger,
        IChatClient chatClient)
    {
        _services = services;
        _logger = logger;
        _chatClient = chatClient;
    }
    
    /// <summary>
    /// Check if coordinator mode is active.
    /// </summary>
    public bool IsCoordinatorMode()
    {
        return Environment.GetEnvironmentVariable("HERMES_COORDINATOR_MODE") == "true";
    }
    
    /// <summary>
    /// Match session mode on resume.
    /// Switches coordinator mode to match the session's mode.
    /// </summary>
    public string? MatchSessionMode(string? sessionMode)
    {
        var currentMode = IsCoordinatorMode() ? "coordinator" : "normal";
        
        if (currentMode != sessionMode)
        {
            // Switch mode to match session
            if (sessionMode == "coordinator")
            {
                Environment.SetEnvironmentVariable("HERMES_COORDINATOR_MODE", "true");
                _logger.LogInformation("Switched to coordinator mode to match session");
            }
            else
            {
                Environment.SetEnvironmentVariable("HERMES_COORDINATOR_MODE", null);
                _logger.LogInformation("Switched to normal mode to match session");
            }
            
            return $"Switched to {sessionMode} mode to match session";
        }
        
        return null; // No change needed
    }
    
    /// <summary>
    /// Get coordinator system prompt.
    /// Describes coordinator role, workflow, and guidelines.
    /// </summary>
    public string GetCoordinatorSystemPrompt()
    {
        return @"
You are a COORDINATOR orchestrating multiple worker agents.

## Your Role
- Break complex tasks into subtasks
- Assign subtasks to workers via Agent tool
- Monitor worker progress
- Synthesize worker outputs
- Ensure quality and completeness

## Available Tools
- Agent: Spawn worker agents
- SendMessage: Communicate with workers
- TaskStop: Stop misbehaving workers
- SyntheticOutput: Return structured results

## Workflow
1. **Research**: Understand the task
2. **Synthesis**: Plan the approach
3. **Implementation**: Assign subtasks
4. **Verification**: Review results

## Worker Prompt Guidelines
- Be specific and actionable
- Include success criteria
- Specify output format
- Set clear constraints
- Provide context and examples

## Concurrency Strategy
- Spawn workers in parallel when tasks are independent
- Sequence workers when there are dependencies
- Monitor progress and reallocate as needed

## Example Session

User: Refactor the authentication module to use JWT

Coordinator: I'll break this into parallel subtasks:

1. Research current auth implementation
2. Design JWT schema
3. Implement JWT utilities
4. Update auth endpoints
5. Write tests

Let me spawn workers for each subtask...

[spawns 5 Agent tools in parallel]

[monitors progress, resolves conflicts]

[synthesizes outputs into cohesive result]
".Trim();
    }
    
    /// <summary>
    /// Get coordinator user context.
    /// Includes worker tools, MCP servers, scratchpad.
    /// </summary>
    public Dictionary<string, string> GetCoordinatorUserContext(
        List<string> workerTools,
        List<string> mcpServers,
        string? scratchpadDir)
    {
        var context = new Dictionary<string, string>();
        
        var toolsContext = $@"
Available worker tools:
{string.Join("\n", workerTools)}

MCP Servers: {string.Join(", ", mcpServers)}
";
        
        if (!string.IsNullOrEmpty(scratchpadDir))
        {
            toolsContext += $"\nScratchpad: {scratchpadDir}";
        }
        
        context["workerToolsContext"] = toolsContext.Trim();
        
        return context;
    }
}

/// <summary>
/// Task workflow phases.
/// </summary>
public enum TaskWorkflowPhase
{
    Research,
    Synthesis,
    Implementation,
    Verification
}

/// <summary>
/// Worker prompt guidelines.
/// </summary>
public static class WorkerPromptGuidelines
{
    public static readonly string Guidelines = @"
## Writing Effective Worker Prompts

1. **Be Specific**: Clearly state what needs to be done
2. **Success Criteria**: Define what done looks like
3. **Output Format**: Specify how results should be formatted
4. **Constraints**: List any limitations or requirements
5. **Context**: Provide relevant background information
6. **Examples**: Include examples when helpful
";
}
