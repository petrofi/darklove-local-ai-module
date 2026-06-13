using System.Diagnostics;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;
using Microsoft.Extensions.Options;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public sealed class LmStudioRuntimeLauncher(
    IOptions<LocalModelOptions> options,
    ILogger<LmStudioRuntimeLauncher> logger) : ILocalModelRuntimeLauncher
{
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private readonly LocalModelOptions _options = options.Value;

    public async Task<bool> EnsureRunningAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_options.AutoStartRuntime)
        {
            return false;
        }

        await _startLock.WaitAsync(cancellationToken);

        try
        {
            var command = FindLmsCommand();

            if (command is null)
            {
                logger.LogWarning("LM Studio CLI bulunamadı; çalışma zamanı otomatik başlatılamadı.");
                return false;
            }

            var endpoint = new Uri(_options.Endpoint);
            var daemonStarted = await RunAsync(
                command,
                ["daemon", "up"],
                cancellationToken);

            if (!daemonStarted)
            {
                return false;
            }

            return await RunAsync(
                command,
                [
                    "server",
                    "start",
                    "--port",
                    endpoint.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "--bind",
                    "127.0.0.1"
                ],
                cancellationToken);
        }
        finally
        {
            _startLock.Release();
        }
    }

    private string? FindLmsCommand()
    {
        if (!string.IsNullOrWhiteSpace(_options.RuntimeCommand))
        {
            return _options.RuntimeCommand;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var executableName = OperatingSystem.IsWindows() ? "lms.exe" : "lms";
        var userCommand = Path.Combine(userProfile, ".lmstudio", "bin", executableName);

        return File.Exists(userCommand) ? userCommand : executableName;
    }

    private async Task<bool> RunAsync(
        string command,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            process.Start();

            var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardError = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await standardOutput;
            var error = await standardError;

            if (process.ExitCode == 0)
            {
                return true;
            }

            logger.LogWarning(
                "LM Studio komutu başarısız oldu. ExitCode: {ExitCode}, Error: {Error}",
                process.ExitCode,
                string.IsNullOrWhiteSpace(error) ? output : error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
                System.ComponentModel.Win32Exception or
                OperationCanceledException)
        {
            logger.LogWarning(exception, "LM Studio çalışma zamanı başlatılamadı.");
        }

        return false;
    }
}
