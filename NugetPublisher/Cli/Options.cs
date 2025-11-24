using System.Globalization;
using System.Text.RegularExpressions;

namespace NugetPublisher.Cli;

internal sealed record Options
{
    public required string ProjectFilePath { get; init; }
    public string? PackageName { get; init; }
    public string? VersionFilePath { get; init; }
    public string? VersionRegex { get; init; }
    public RegexOptions VersionRegexOptions { get; init; }
    public string? VersionStatic { get; init; }
    public bool TagCommit { get; init; }
    public string TagFormat { get; init; } = "v*";
    public string NugetSource { get; init; } = "https://api.nuget.org";
    public string NugetPushSource { get; init; } = "https://api.nuget.org/v3/index.json";
    public string? NugetApiKey { get; init; }
    public bool IncludeSymbols { get; init; }
    public string Configuration { get; init; } = "Release";
    public required string EffectiveOutputDirectory { get; init; }
    public bool CleanBeforeBuild { get; init; }
    public bool PublishToGitHubPackages { get; init; }
    public string? GitHubPackagesOwner { get; init; }
    public string? GitHubPackagesSource { get; init; }
    public string? GitHubPackagesApiKey { get; init; }
    public bool GitHubPackagesIncludeSymbols { get; init; }
    public string WorkingDirectory { get; init; } = Directory.GetCurrentDirectory();
    public string GitUserName { get; init; } = "github-actions";
    public string GitUserEmail { get; init; } = "actions@github.com";
    public bool DryRun { get; init; }
    public bool SkipWhenVersionExists { get; init; }
    public string? ExtraPackArguments { get; init; }

    public static Options FromEnvironment()
    {
        var repoRoot = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        var workingDirectory = string.IsNullOrWhiteSpace(repoRoot) ? Directory.GetCurrentDirectory() : repoRoot;
        var projectFile = RequireEnv("PROJECT_FILE");
        var projectFullPath = Path.GetFullPath(projectFile, workingDirectory);

        var outputDirectory = Environment.GetEnvironmentVariable("OUTPUT_DIRECTORY");
        var effectiveOutputDirectory = string.IsNullOrWhiteSpace(outputDirectory) ? Path.Combine(Path.GetDirectoryName(projectFullPath) ?? workingDirectory, "nupkg") : Path.GetFullPath(outputDirectory, workingDirectory);

        var versionFile = Environment.GetEnvironmentVariable("VERSION_FILE");
        string? versionFilePath = null;
        if (!string.IsNullOrWhiteSpace(versionFile))
        {
            versionFilePath = Path.GetFullPath(versionFile, workingDirectory);
        }

        return new()
        {
            ProjectFilePath = projectFullPath,
            PackageName = GetEnv("PACKAGE_NAME"),
            VersionFilePath = versionFilePath,
            VersionRegex = GetEnv("VERSION_REGEX"),
            VersionRegexOptions = ParseRegexOptions(GetEnv("VERSION_REGEX_OPTIONS")),
            VersionStatic = GetEnv("VERSION_STATIC"),
            TagCommit = GetBool("TAG_COMMIT", true),
            TagFormat = GetEnv("TAG_FORMAT") ?? "v*",
            NugetSource = GetEnv("NUGET_SOURCE") ?? "https://api.nuget.org",
            NugetPushSource = GetEnv("NUGET_PUSH_SOURCE") ?? "https://api.nuget.org/v3/index.json",
            NugetApiKey = GetEnv("NUGET_API_KEY"),
            IncludeSymbols = GetBool("INCLUDE_SYMBOLS"),
            Configuration = GetEnv("DOTNET_CONFIGURATION") ?? "Release",
            EffectiveOutputDirectory = effectiveOutputDirectory,
            CleanBeforeBuild = GetBool("CLEAN_BEFORE_BUILD", true),
            PublishToGitHubPackages = GetBool("PUBLISH_TO_GITHUB_PACKAGES"),
            GitHubPackagesOwner = GetEnv("GITHUB_PACKAGES_OWNER"),
            GitHubPackagesSource = GetEnv("GITHUB_PACKAGES_SOURCE"),
            GitHubPackagesApiKey = GetEnv("GITHUB_PACKAGES_API_KEY"),
            GitHubPackagesIncludeSymbols = GetBool("GITHUB_PACKAGES_INCLUDE_SYMBOLS"),
            GitUserName = GetEnv("GIT_USER_NAME") ?? "github-actions",
            GitUserEmail = GetEnv("GIT_USER_EMAIL") ?? "actions@github.com",
            DryRun = GetBool("DRY_RUN"),
            SkipWhenVersionExists = GetBool("SKIP_WHEN_VERSION_EXISTS", true),
            ExtraPackArguments = GetEnv("EXTRA_PACK_ARGUMENTS"),
            WorkingDirectory = workingDirectory
        };
    }

    private static string RequireEnv(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Environment variable '{name}' must be provided.");
        }

        return value;
    }

    private static string? GetEnv(string name) => Environment.GetEnvironmentVariable(name);

    private static bool GetBool(string name, bool defaultValue = false)
    {
        var raw = GetEnv(name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        if (bool.TryParse(raw, out var parsed))
        {
            return parsed;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric != 0;
        }

        return defaultValue;
    }

    private static RegexOptions ParseRegexOptions(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return RegexOptions.Multiline;
        }

        var result = RegexOptions.None;
        var parts = value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (!Enum.TryParse(part, true, out RegexOptions parsed))
            {
                throw new InvalidOperationException($"Unknown RegexOptions flag '{part}'.");
            }

            result |= parsed;
        }

        return result;
    }
}
