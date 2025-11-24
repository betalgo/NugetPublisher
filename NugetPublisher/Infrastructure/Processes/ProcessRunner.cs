using System.Diagnostics;
using System.Text;
using NugetPublisher.Common;

namespace NugetPublisher.Infrastructure.Processes;

internal static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(string fileName, IEnumerable<string> arguments, string workingDirectory, IEnumerable<string>? secrets = null, bool throwOnError = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var argsList = arguments.ToList();
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in argsList)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var displayArgs = string.Join(" ", argsList.Select(QuoteIfNeeded));
        displayArgs = Redact(displayArgs, secrets);
        Logger.Info($"$ {fileName} {displayArgs}".TrimEnd());

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                return;
            }

            stdOut.AppendLine(e.Data);
            Console.WriteLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                return;
            }

            stdErr.AppendLine(e.Data);
            Console.Error.WriteLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        var result = new ProcessResult(process.ExitCode, stdOut.ToString(), stdErr.ToString());
        if (throwOnError && result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command '{fileName}' exited with code {result.ExitCode}.");
        }

        return result;
    }

    private static string QuoteIfNeeded(string value) => value.Contains(' ', StringComparison.Ordinal) ? $"\"{value}\"" : value;

    private static string Redact(string input, IEnumerable<string>? secrets)
    {
        if (secrets is null)
        {
            return input;
        }

        var result = input;
        foreach (var secret in secrets.Where(s => !string.IsNullOrEmpty(s)))
        {
            result = result.Replace(secret, "***", StringComparison.Ordinal);
        }

        return result;
    }
}
