namespace Hermes.Agent.Agents;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// Agent service - spawn and manage subagents.
/// Supports worktree and remote isolation strategies.
/// </summary>

public sealed class AgentService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AgentService> _logger;
    private readonly string _worktreesDir;
    
    public AgentService(IServiceProvider services, ILogger<AgentService> logger, string worktreesDir)
    {
        _services = services;
        _logger = logger;
        _worktreesDir = worktreesDir;
        
        Directory.CreateDirectory(worktreesDir);
    }
    
    /// <summary>
    /// Spawn a subagent.
    /// </summary>
    public async Task<AgentResult> SpawnAgentAsync(AgentRequest request, CancellationToken ct)
    {
        var agentId = GenerateAgentId();
        _logger.LogInformation("Spawning agent {AgentId} for task: {Description}", agentId, request.Description);
        
        try
        {
            // Determine isolation strategy
            var isolation = request.Isolation?.ToLower() switch
            {
                "worktree" => await CreateWorktreeIsolationAsync(request, ct),
                "remote" => await CreateRemoteIsolationAsync(request, ct),
                _ => IsolationStrategy.None
            };
            
            // Build agent context
            var context = new AgentContext
            {
                AgentId = agentId,
                Prompt = request.Prompt,
                Model = request.Model ?? "default",
                WorkingDirectory = isolation.WorkingDirectory ?? Environment.CurrentDirectory,
                IsSubagent = true,
                ParentAgentId = GetCurrentAgentId(),
                TeamName = request.TeamName
            };
            
            // Spawn agent
            var agent = new AgentRunner(context, _services);
            
            if (request.RunInBackground)
            {
                // Fire and forget
                _ = agent.RunAsync(ct);
                _logger.LogInformation("Agent {AgentId} spawned in background", agentId);
                
                return new AgentResult 
                { 
                    AgentId = agentId, 
                    Status = "spawned",
                    BackgroundTaskId = agentId // Use agent ID as task ID
                };
            }
            else
            {
                // Wait for completion
                var result = await agent.RunAsync(ct);
                _logger.LogInformation("Agent {AgentId} completed with status: {Status}", agentId, result.Status);
                
                return new AgentResult 
                { 
                    AgentId = agentId, 
                    Status = result.Status,
                    Output = result.Output
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to spawn agent {AgentId}", agentId);
            return new AgentResult 
            { 
                AgentId = agentId, 
                Status = "failed",
                Error = ex.Message
            };
        }
    }
    
    /// <summary>
    /// Create worktree isolation for agent.
    /// </summary>
    private async Task<IsolationStrategy> CreateWorktreeIsolationAsync(
        AgentRequest request, CancellationToken ct)
    {
        var worktreeName = $"agent-{Guid.NewGuid():N}";
        var worktreePath = Path.Combine(_worktreesDir, worktreeName);
        
        try
        {
            // Create git worktree
            _logger.LogInformation("Creating worktree at {Path}", worktreePath);
            
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"worktree add {worktreePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = new Process { StartInfo = psi };
            process.Start();
            await process.WaitForExitAsync(ct);
            
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                throw new InvalidOperationException($"Git worktree failed: {error}");
            }
            
            return new IsolationStrategy
            {
                Type = "worktree",
                WorkingDirectory = worktreePath,
                Cleanup = async () =>
                {
                    _logger.LogInformation("Cleaning up worktree {Path}", worktreePath);
                    
                    var removePsi = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"worktree remove {worktreePath}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var removeProc = new Process { StartInfo = removePsi };
                    removeProc.Start();
                    await removeProc.WaitForExitAsync(ct);
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create worktree");
            return IsolationStrategy.None;
        }
    }
    
    /// <summary>
    /// Create remote isolation for agent.
    /// </summary>
    private async Task<IsolationStrategy> CreateRemoteIsolationAsync(
        AgentRequest request, CancellationToken ct)
    {
        // TODO: Implement remote session creation via Remote Control API
        // For now, return none
        _logger.LogWarning("Remote isolation not yet implemented");
        return IsolationStrategy.None;
    }
    
    private string GenerateAgentId() => $"agent_{Guid.NewGuid():N}";
    private string? GetCurrentAgentId() => null; // TODO: Track current agent ID
}

// =============================================
// Team Manager
// =============================================

/// <summary>
/// Team management - create, delete, list teams.
/// Teams are persistent agent swarms with shared context.
/// </summary>
public sealed class TeamManager
{
    private readonly string _teamsDir;
    private readonly MailboxService _mailbox;
    private readonly ILogger<TeamManager> _logger;
    
    public TeamManager(string teamsDir, MailboxService mailbox, ILogger<TeamManager> logger)
    {
        _teamsDir = teamsDir;
        _mailbox = mailbox;
        _logger = logger;
        
        Directory.CreateDirectory(teamsDir);
    }
    
    /// <summary>
    /// Create a new team.
    /// One team per leader enforced.
    /// </summary>
    public async Task<TeamResult> CreateTeamAsync(
        string teamName, 
        string? description,
        CancellationToken ct)
    {
        var teamPath = Path.Combine(_teamsDir, $"{teamName}.json");
        
        if (File.Exists(teamPath))
        {
            throw new TeamAlreadyExistsException(teamName);
        }
        
        var team = new Team
        {
            TeamName = teamName,
            Description = description,
            LeadAgentId = GetCurrentAgentId() ?? "main-thread",
            Members = new List<TeamMember>(),
            CreatedAt = DateTime.UtcNow
        };
        
        var json = JsonSerializer.Serialize(team, JsonOptions);
        await File.WriteAllTextAsync(teamPath, json, ct);
        
        _logger.LogInformation("Created team {TeamName} with lead {LeadId}", teamName, team.LeadAgentId);
        
        return new TeamResult
        {
            TeamName = teamName,
            TeamFilePath = teamPath,
            LeadAgentId = team.LeadAgentId
        };
    }
    
    /// <summary>
    /// Delete a team.
    /// Refuses if any non-lead members are still running.
    /// </summary>
    public async Task DeleteTeamAsync(string teamName, CancellationToken ct)
    {
        var team = await LoadTeamAsync(teamName, ct);
        
        // Check for running members
        var runningMembers = team.Members
            .Where(m => m.Status == "active" && m.AgentId != team.LeadAgentId)
            .ToList();
        
        if (runningMembers.Any())
        {
            throw new TeamHasRunningMembersException(
                $"Cannot delete team with {runningMembers.Count} running members");
        }
        
        // Cleanup worktrees
        await CleanupTeamWorktreesAsync(teamName, ct);
        
        // Delete team file
        var teamPath = Path.Combine(_teamsDir, $"{teamName}.json");
        if (File.Exists(teamPath))
        {
            File.Delete(teamPath);
        }
        
        _logger.LogInformation("Deleted team {TeamName}", teamName);
    }
    
    /// <summary>
    /// Add member to team.
    /// </summary>
    public async Task AddMemberAsync(string teamName, TeamMember member, CancellationToken ct)
    {
        var team = await LoadTeamAsync(teamName, ct);
        team.Members.Add(member);
        await SaveTeamAsync(team, ct);
        
        _logger.LogInformation("Added member {MemberId} to team {TeamName}", member.AgentId, teamName);
    }
    
    /// <summary>
    /// Update member status.
    /// </summary>
    public async Task UpdateMemberStatusAsync(
        string teamName, 
        string agentId, 
        string status, 
        CancellationToken ct)
    {
        var team = await LoadTeamAsync(teamName, ct);
        var member = team.Members.FirstOrDefault(m => m.AgentId == agentId);
        
        if (member != null)
        {
            member.Status = status;
            await SaveTeamAsync(team, ct);
            
            _logger.LogInformation("Updated member {AgentId} status to {Status}", agentId, status);
        }
    }
    
    /// <summary>
    /// Load team from disk.
    /// </summary>
    private async Task<Team> LoadTeamAsync(string teamName, CancellationToken ct)
    {
        var teamPath = Path.Combine(_teamsDir, $"{teamName}.json");
        
        if (!File.Exists(teamPath))
            throw new TeamNotFoundException(teamName);
        
        var json = await File.ReadAllTextAsync(teamPath, ct);
        return JsonSerializer.Deserialize<Team>(json, JsonOptions) 
            ?? throw new TeamNotFoundException(teamName);
    }
    
    /// <summary>
    /// Save team to disk.
    /// </summary>
    private async Task SaveTeamAsync(Team team, CancellationToken ct)
    {
        var teamPath = Path.Combine(_teamsDir, $"{team.TeamName}.json");
        var json = JsonSerializer.Serialize(team, JsonOptions);
        await File.WriteAllTextAsync(teamPath, json, ct);
    }
    
    /// <summary>
    /// Cleanup team worktrees.
    /// </summary>
    private async Task CleanupTeamWorktreesAsync(string teamName, CancellationToken ct)
    {
        var team = await LoadTeamAsync(teamName, ct);
        
        foreach (var member in team.Members)
        {
            if (member.WorktreePath != null && Directory.Exists(member.WorktreePath))
            {
                try
                {
                    _logger.LogInformation("Removing worktree {Path}", member.WorktreePath);
                    Directory.Delete(member.WorktreePath, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove worktree {Path}", member.WorktreePath);
                }
            }
        }
    }
    
    private string? GetCurrentAgentId() => null; // TODO: Track current agent ID
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}

// =============================================
// Mailbox Service (Inter-agent messaging)
// =============================================

/// <summary>
/// Mailbox service for inter-agent messaging.
/// Messages persist to ~/.hermes-cs/mailboxes/<agent-name>.json
/// </summary>
public sealed class MailboxService
{
    private readonly string _mailboxDir;
    private readonly ILogger<MailboxService> _logger;
    
    public MailboxService(string mailboxDir, ILogger<MailboxService> logger)
    {
        _mailboxDir = mailboxDir;
        _logger = logger;
        
        Directory.CreateDirectory(mailboxDir);
    }
    
    /// <summary>
    /// Send message to agent/teammate.
    /// </summary>
    public async Task SendMessageAsync(
        string recipient,
        string message,
        string? fromAgentId,
        CancellationToken ct)
    {
        var mailboxPath = Path.Combine(_mailboxDir, $"{recipient}.json");
        
        var msg = new MailboxMessage
        {
            From = fromAgentId ?? "unknown",
            Content = message,
            Timestamp = DateTime.UtcNow,
            Read = false
        };
        
        // Load or create mailbox
        var mailbox = await LoadMailboxAsync(mailboxPath, ct);
        mailbox.Messages.Add(msg);
        
        // Save
        await SaveMailboxAsync(mailboxPath, mailbox, ct);
        
        _logger.LogInformation("Sent message to {Recipient} from {From}", recipient, fromAgentId);
    }
    
    /// <summary>
    /// Read messages for agent.
    /// Marks messages as read.
    /// </summary>
    public async Task<List<MailboxMessage>> ReadMessagesAsync(
        string agentName,
        CancellationToken ct)
    {
        var mailboxPath = Path.Combine(_mailboxDir, $"{agentName}.json");
        
        if (!File.Exists(mailboxPath))
            return new List<MailboxMessage>();
        
        var mailbox = await LoadMailboxAsync(mailboxPath, ct);
        
        // Mark as read
        foreach (var msg in mailbox.Messages.Where(m => !m.Read))
        {
            msg.Read = true;
        }
        
        await SaveMailboxAsync(mailboxPath, mailbox, ct);
        
        return mailbox.Messages.OrderByDescending(m => m.Timestamp).ToList();
    }
    
    /// <summary>
    /// Clear mailbox.
    /// </summary>
    public async Task ClearMailboxAsync(string agentName, CancellationToken ct)
    {
        var mailboxPath = Path.Combine(_mailboxDir, $"{agentName}.json");
        
        if (File.Exists(mailboxPath))
        {
            File.Delete(mailboxPath);
            _logger.LogInformation("Cleared mailbox for {AgentName}", agentName);
        }
    }
    
    private async Task<Mailbox> LoadMailboxAsync(string path, CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<Mailbox>(json, JsonOptions) ?? new Mailbox();
    }
    
    private async Task SaveMailboxAsync(string path, Mailbox mailbox, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(mailbox, JsonOptions);
        await File.WriteAllTextAsync(path, json, ct);
    }
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}

// =============================================
// Types
// =============================================

public sealed class AgentRequest
{
    public required string Description { get; init; }  // 3-5 words
    public required string Prompt { get; init; }       // Full task prompt
    public string? Model { get; init; }                // sonnet, opus, haiku
    public bool RunInBackground { get; init; }
    public string? Name { get; init; }                 // Named agent
    public string? TeamName { get; init; }             // Associate with team
    public string? Isolation { get; init; }            // worktree, remote
}

public sealed class AgentResult
{
    public required string AgentId { get; init; }
    public required string Status { get; init; }       // spawned, completed, failed
    public string? Output { get; init; }
    public string? Error { get; init; }
    public string? BackgroundTaskId { get; init; }
}

public sealed class AgentContext
{
    public required string AgentId { get; init; }
    public required string Prompt { get; init; }
    public required string Model { get; init; }
    public required string WorkingDirectory { get; init; }
    public bool IsSubagent { get; init; }
    public string? ParentAgentId { get; init; }
    public string? TeamName { get; init; }
}

public sealed class IsolationStrategy
{
    public required string Type { get; init; }
    public string? WorkingDirectory { get; init; }
    public Func<Task>? Cleanup { get; init; }
    
    public static IsolationStrategy None => new() { Type = "none" };
}

public sealed class Team
{
    public required string TeamName { get; init; }
    public string? Description { get; init; }
    public required string LeadAgentId { get; init; }
    public List<TeamMember> Members { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public sealed class TeamMember
{
    public required string AgentId { get; init; }
    public required string Name { get; init; }
    public required string Role { get; init; }
    public string Status { get; set; }  // active, idle, completed
    public string? WorktreePath { get; init; }
}

public sealed class TeamResult
{
    public required string TeamName { get; init; }
    public required string TeamFilePath { get; init; }
    public required string LeadAgentId { get; init; }
}

public sealed class Mailbox
{
    public List<MailboxMessage> Messages { get; init; } = new();
}

public sealed class MailboxMessage
{
    public required string From { get; init; }
    public required string Content { get; init; }
    public DateTime Timestamp { get; init; }
    public bool Read { get; set; }
}

// =============================================
// Exceptions
// =============================================

public sealed class TeamAlreadyExistsException : Exception
{
    public TeamAlreadyExistsException(string teamName) 
        : base($"Team '{teamName}' already exists. One team per leader enforced.")
    {
    }
}

public sealed class TeamNotFoundException : Exception
{
    public TeamNotFoundException(string teamName) 
        : base($"Team '{teamName}' not found")
    {
    }
}

public sealed class TeamHasRunningMembersException : Exception
{
    public TeamHasRunningMembersException(string message) : base(message) { }
}

public sealed class AgentRunner
{
    private readonly AgentContext _context;
    private readonly IServiceProvider _services;
    
    public AgentRunner(AgentContext context, IServiceProvider services)
    {
        _context = context;
        _services = services;
    }
    
    public async Task<AgentResult> RunAsync(CancellationToken ct)
    {
        // TODO: Implement agent runner
        // This would spawn a new agent instance with the given context
        // For now, return placeholder
        return new AgentResult
        {
            AgentId = _context.AgentId,
            Status = "not-implemented",
            Output = "Agent runner not yet implemented"
        };
    }
}
