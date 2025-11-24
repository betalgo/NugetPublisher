using NugetPublisher.Infrastructure.Processes;

namespace NugetPublisher.Infrastructure.Git;

internal static class TagHelper
{
    public static string FormatTagName(string template, string version)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return version;
        }

        return template.Contains('*', StringComparison.Ordinal) ? template.Replace("*", version, StringComparison.Ordinal) : $"{template}{version}";
    }

    public static async Task<bool> TagExistsAsync(string tagName, string workingDirectory)
    {
        var result = await ProcessRunner.RunAsync("git", ["rev-parse", "-q", "--verify", $"refs/tags/{tagName}"], workingDirectory, throwOnError: false);
        return result.ExitCode == 0;
    }

    public static Task ConfigureGitIdentityAsync(string userName, string userEmail, string workingDirectory) => Task.WhenAll(ProcessRunner.RunAsync("git", ["config", "user.name", userName], workingDirectory),
        ProcessRunner.RunAsync("git", ["config", "user.email", userEmail], workingDirectory));

    public static async Task CreateAndPushTagAsync(string tagName, string workingDirectory)
    {
        await ProcessRunner.RunAsync("git", ["tag", tagName], workingDirectory);
        await ProcessRunner.RunAsync("git", ["push", "origin", tagName], workingDirectory);
    }
}
