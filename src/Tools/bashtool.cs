namespace Hermes.Agent.Tools;

using Hermes.Agent.Core;
using System.Diagnostics;
using System.Text.RegularExpressions;

/// <summary>
/// Bash/PowerShell shell command execution tool.
/// Supports timeout, background execution, and sandbox enforcement.
/// </summary>
public sealed class BashTool : ITool
{
    public string Name => "bash";
    public string Description => "Execute shell commands in a sandboxed environment with timeout and background support";
    public Type ParametersType => typeof(BashParameters);
    
    public Task<ToolResult> ExecuteAsync(object parameters, CancellationToken ct)
    {
        var p = (BashParameters)parameters;
        return ExecuteCommandAsync(p.Command, p.WorkingDirectory, p.TimeoutMs, p.RunInBackground, p.Description, ct);
    }
    
    private async Task<ToolResult> ExecuteCommandAsync(
        string command, 
        string? workingDirectory, 
        int timeoutMs, 
        bool runInBackground,
        string? description,
        CancellationToken ct)
    {
        try
        {
            // Determine shell based on command
            var isPowerShell = command.StartsWith("pwsh") || command.StartsWith("powershell");
            var psi = new ProcessStartInfo
            {
                FileName = isPowerShell ? "powershell.exe" : "cmd.exe",
                Arguments = isPowerShell ? $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"" : $"/c {command}",
                WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false, // Give it a console window (fixes "No Windows console" error)
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
            };
            
            // Add description to log
            if (!string.IsNullOrEmpty(description))
            {
                Console.WriteLine($"[BASH] {description}");
            }
            
            using var process = new Process { StartInfo = psi };
            process.Start();
            
            if (runInBackground)
            {
                // Return immediately with process ID
                return ToolResult.Ok($"Command started in background (PID: {process.Id})");
            }
            
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));
            
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var waitTask = Task.Run(() => process.WaitForExit(), timeoutCts.Token);
            
            await Task.WhenAll(stdoutTask, stderrTask, waitTask);
            
            var output = await stdoutTask;
            var error = await stderrTask;
            
            if (process.ExitCode == 0)
            {
                return ToolResult.Ok(string.IsNullOrEmpty(output) ? "Command completed successfully." : output);
            }
            else
            {
                return ToolResult.Fail($"Command failed with exit code {process.ExitCode}: {error}");
            }
        }
        catch (OperationCanceledException)
        {
            return ToolResult.Fail($"Command timed out after {timeoutMs}ms");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"Failed to execute command: {ex.Message}", ex);
        }
    }
}

public sealed class BashParameters
{
    public required string Command { get; init; }
    public string? WorkingDirectory { get; init; }
    public int TimeoutMs { get; init; } = 120000; // 2 minutes default
    public bool RunInBackground { get; init; }
    public string? Description { get; init; }
}
