namespace Hermes.Agent.Tasks;

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Task V2 - Full project management with dependencies, priorities, and verification.
/// </summary>

public sealed class TaskManager
{
    private readonly string _tasksDir;
    private readonly ILogger<TaskManager> _logger;
    private readonly ConcurrentDictionary<string, HermesTask> _tasks = new();
    
    public TaskManager(string tasksDir, ILogger<TaskManager> logger)
    {
        _tasksDir = tasksDir;
        _logger = logger;
        Directory.CreateDirectory(tasksDir);
        LoadTasks();
    }
    
    public Task<TaskResult> CreateTaskAsync(TaskCreateRequest request, CancellationToken ct)
    {
        var taskId = $"task_{Guid.NewGuid():N}";
        var task = new HermesTask
        {
            TaskId = taskId,
            Description = request.Description,
            Status = request.Status ?? "pending",
            Priority = request.Priority ?? "medium",
            Assignee = request.Assignee,
            DueDate = request.DueDate,
            Dependencies = request.Dependencies ?? new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _tasks[taskId] = task;
        _ = SaveTaskAsync(task, ct);
        
        _logger.LogInformation("Created task {TaskId}: {Description}", taskId, request.Description);
        
        return Task.FromResult(new TaskResult { TaskId = taskId, Status = task.Status, Message = $"Task created: {task.Description}" });
    }
    
    public HermesTask? GetTask(string taskId) => _tasks.TryGetValue(taskId, out var task) ? task : null;
    public List<HermesTask> ListTasks() => _tasks.Values.OrderByDescending(t => t.CreatedAt).ToList();
    
    private void LoadTasks()
    {
        if (!Directory.Exists(_tasksDir)) return;
        foreach (var file in Directory.EnumerateFiles(_tasksDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var task = JsonSerializer.Deserialize<HermesTask>(json, JsonOptions);
                if (task != null) _tasks[task.TaskId] = task;
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load task from {File}", file); }
        }
    }
    
    private Task SaveTaskAsync(HermesTask task, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(task, JsonOptions);
        return File.WriteAllTextAsync(Path.Combine(_tasksDir, $"{task.TaskId}.json"), json, ct);
    }
    
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
}

public sealed class HermesTask { public required string TaskId { get; init; } public required string Description { get; set; } public required string Status { get; set; } public required string Priority { get; set; } public string? Assignee { get; set; } public DateTime? DueDate { get; set; } public List<string> Dependencies { get; set; } = new(); public DateTime CreatedAt { get; init; } public DateTime UpdatedAt { get; set; } }
public sealed class TaskCreateRequest { public required string Description { get; init; } public string? Status { get; init; } public string? Priority { get; init; } public string? Assignee { get; init; } public DateTime? DueDate { get; init; } public List<string>? Dependencies { get; init; } }
public sealed class TaskResult { public required string TaskId { get; init; } public required string Status { get; init; } public required string Message { get; init; } }
public sealed class TaskNotFoundException(string taskId) : Exception($"Task '{taskId}' not found");
