namespace Hermes.Agent.Tools;

using Hermes.Agent.Core;
using Hermes.Agent.Security;
using System.Diagnostics;

public sealed class TerminalTool : ITool
{
    private readonly ShellSecurityAnalyzer _securityAnalyzer;
    private readonly TerminalSecurityPolicy _policy;

    public string Name => "terminal";
    public string Description => "Execute shell commands on the local system";
    public Type ParametersType => typeof(TerminalParameters);

    public TerminalTool(TerminalSecurityPolicy? policy = null)
    {
        _securityAnalyzer = new ShellSecurityAnalyzer();
        _policy = policy ?? new TerminalSecurityPolicy();
    }
    
    public Task<ToolResult> ExecuteAsync(object parameters, CancellationToken ct)
    {
        var p = (TerminalParameters)parameters;
        return ExecuteCommandAsync(p.Command, p.WorkingDirectory, p.TimeoutSeconds, ct);
    }
    
    private async Task<ToolResult> ExecuteCommandAsync(string command, string? cwd, int timeout, CancellationToken ct)
    {
        // Security analysis — mirrors BashTool's 22 validators
        var context = new ShellContext
        {
            WorkingDirectory = cwd,
            IsSandboxed = _policy.IsSandboxed,
            AllowNetwork = _policy.AllowNetwork,
            AllowFileSystemWrite = _policy.AllowFileSystemWrite,
            AllowSubprocess = _policy.AllowSubprocess,
        };

        var securityResult = _securityAnalyzer.Analyze(command, context);

        switch (securityResult.Classification)
        {
            case SecurityClassification.Dangerous:
                return ToolResult.Fail($"Security: {securityResult.Reason}");

            case SecurityClassification.TooComplex:
                return ToolResult.Fail($"Security: Command too complex to analyze - {securityResult.Reason}");

            case SecurityClassification.NeedsReview:
                if (!_policy.AutoApproveWarnings)
                {
                    var warnings = string.Join("\n", securityResult.Warnings ?? new List<string> { securityResult.Reason ?? "Unknown" });
                    return ToolResult.Fail($"Security review required:\n{warnings}");
                }
                break;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                WorkingDirectory = cwd ?? Directory.GetCurrentDirectory(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
            };
            
            using var process = new Process { StartInfo = psi };
            process.Start();
            
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeout));
            
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var waitTask = Task.Run(() => process.WaitForExit(), timeoutCts.Token);
            
            await Task.WhenAll(stdoutTask, stderrTask, waitTask);
            
            var output = await stdoutTask;
            var error = await stderrTask;
            
            return process.ExitCode == 0 
                ? ToolResult.Ok(string.IsNullOrEmpty(output) ? "Command completed successfully." : output)
                : ToolResult.Fail($"Command failed with exit code {process.ExitCode}: {error}");
        }
        catch (OperationCanceledException)
        {
            return ToolResult.Fail($"Command timed out after {timeout} seconds");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"Failed to execute command: {ex.Message}", ex);
        }
    }
}

public sealed class TerminalParameters
{
    public required string Command { get; init; }
    public string? WorkingDirectory { get; init; }
    public int TimeoutSeconds { get; init; } = 60;
}

/// <summary>
/// Security policy for TerminalTool. Mirrors BashSecurityPolicy.
/// </summary>
public sealed class TerminalSecurityPolicy
{
    public bool IsSandboxed { get; init; }
    public bool AllowNetwork { get; init; } = true;
    public bool AllowFileSystemWrite { get; init; } = true;
    public bool AllowSubprocess { get; init; } = true;
    public bool AutoApproveWarnings { get; init; } = false;
}
