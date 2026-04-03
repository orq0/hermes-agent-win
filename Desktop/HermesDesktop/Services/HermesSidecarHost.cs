using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HermesDesktop.Services;

internal sealed class HermesSidecarHost
{
    private static readonly HttpClient Client = new();
    private Process? _process;

    internal string BaseUrl => "http://127.0.0.1:8765/";

    internal async Task EnsureStartedAsync()
    {
        if (await IsHealthyAsync())
        {
            return;
        }

        StartProcess();

        for (int attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(250);
            if (await IsHealthyAsync())
            {
                return;
            }
        }

        throw new InvalidOperationException("Hermes sidecar did not start successfully.");
    }

    private async Task<bool> IsHealthyAsync()
    {
        try
        {
            using HttpResponseMessage response = await Client.GetAsync(new Uri(new Uri(BaseUrl), "health"));
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void StartProcess()
    {
        if (_process is { HasExited: false })
        {
            return;
        }

        string scriptPath = Path.Combine(AppContext.BaseDirectory, "sidecar", "hermes_sidecar.py");
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("Hermes sidecar script not found.", scriptPath);
        }

        string executable;
        string arguments;

        if (File.Exists(HermesEnvironment.HermesPythonPath))
        {
            executable = HermesEnvironment.HermesPythonPath;
            arguments = $"\"{scriptPath}\" --port 8765";
        }
        else
        {
            executable = "py";
            arguments = $"-3 \"{scriptPath}\" --port 8765";
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = HermesEnvironment.AgentWorkingDirectory,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        startInfo.Environment["PYTHONUTF8"] = "1";
        startInfo.Environment["HERMES_HOME"] = HermesEnvironment.HermesHomePath;
        startInfo.Environment["HERMES_DESKTOP_WORKSPACE"] = HermesEnvironment.AgentWorkingDirectory;

        _process = Process.Start(startInfo);
        if (_process is null)
        {
            throw new InvalidOperationException("Failed to launch the Hermes sidecar process.");
        }

        _process.OutputDataReceived += (_, _) => { };
        _process.ErrorDataReceived += (_, _) => { };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }
}
