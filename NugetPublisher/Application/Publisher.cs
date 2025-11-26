using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using NugetPublisher.Cli;
using NugetPublisher.Common;
using NugetPublisher.Infrastructure.Git;
using NugetPublisher.Infrastructure.IO;
using NugetPublisher.Infrastructure.Processes;

namespace NugetPublisher.Application;

internal sealed class Publisher(Options options) : IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public void Dispose() => _httpClient.Dispose();

    public async Task ExecuteAsync()
    {
        ValidateInputs();

        var packageName = ResolvePackageName();
        var version = ResolveVersion();

        Logger.Info($"Project: {options.ProjectFilePath}");
        Logger.Info($"Package: {packageName}");
        Logger.Info($"Version: {version}");

        OutputWriter.Set("package_name", packageName);
        OutputWriter.Set("version", version);

        var shouldPublish = !options.SkipWhenVersionExists || await PackageVersionIsMissingAsync(packageName, version);
        OutputWriter.Set("should_publish", shouldPublish.ToString().ToLowerInvariant());

        if (!shouldPublish)
        {
            Logger.Info($"Version {version} already exists on {options.NugetSource}. Skipping publish.");
            OutputWriter.Set("published", "false");
            return;
        }

        if (options.DryRun)
        {
            Logger.Info("Dry-run enabled. Skipping packing and push operations.");
            OutputWriter.Set("published", "false");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.NugetApiKey))
        {
            Logger.Warn("NUGET_API_KEY was not provided; skipping publish.");
            OutputWriter.Set("published", "false");
            return;
        }

        PrepareOutputDirectory();

        if (options.CleanBeforeBuild)
        {
            await ProcessRunner.RunAsync("dotnet", ["clean", options.ProjectFilePath, "-c", options.Configuration], options.WorkingDirectory);
        }

        await PackAsync(packageName, version);

        var packagePath = LocatePackageFile(packageName, version, ".nupkg");
        if (string.IsNullOrEmpty(packagePath))
        {
            throw new InvalidOperationException($"Unable to locate .nupkg file in {options.EffectiveOutputDirectory}.");
        }

        OutputWriter.Set("package_path", packagePath);

        string? symbolsPath = null;
        if (options.IncludeSymbols)
        {
            symbolsPath = LocatePackageFile(packageName, version, ".snupkg");
            if (!string.IsNullOrWhiteSpace(symbolsPath))
            {
                OutputWriter.Set("symbols_package_path", symbolsPath);
            }
        }

        await PushPackageAsync(packagePath, options.NugetPushSource, options.NugetApiKey!, false);

        if (options.IncludeSymbols && !string.IsNullOrWhiteSpace(symbolsPath))
        {
            await PushPackageAsync(symbolsPath!, options.NugetPushSource, options.NugetApiKey!, true);
        }

        if (options.PublishToGitHubPackages)
        {
            var ghSource = ResolveGitHubPackagesSource();
            if (string.IsNullOrWhiteSpace(options.GitHubPackagesApiKey))
            {
                Logger.Warn("GitHub Packages API key/token was not supplied; skipping GitHub Packages push.");
            }
            else if (string.IsNullOrWhiteSpace(ghSource))
            {
                Logger.Warn("GitHub Packages source could not be determined; skipping GitHub Packages push.");
            }
            else
            {
                await PushPackageAsync(packagePath, ghSource!, options.GitHubPackagesApiKey!, false);
                if (options.GitHubPackagesIncludeSymbols && !string.IsNullOrWhiteSpace(symbolsPath))
                {
                    await PushPackageAsync(symbolsPath!, ghSource!, options.GitHubPackagesApiKey!, true);
                }

                OutputWriter.Set("github_packages_source", ghSource!);
            }
        }

        OutputWriter.Set("published", "true");

        if (options.TagCommit)
        {
            var tagName = TagHelper.FormatTagName(options.TagFormat, version);
            if (await TagHelper.TagExistsAsync(tagName, options.WorkingDirectory))
            {
                Logger.Info($"Tag '{tagName}' already exists. Skipping creation.");
            }
            else
            {
                await TagHelper.ConfigureGitIdentityAsync(options.GitUserName, options.GitUserEmail, options.WorkingDirectory);
                await TagHelper.CreateAndPushTagAsync(tagName, options.WorkingDirectory);
                OutputWriter.Set("tag", tagName);
            }
        }
    }

    private void ValidateInputs()
    {
        if (!File.Exists(options.ProjectFilePath))
        {
            throw new FileNotFoundException($"Project file not found: {options.ProjectFilePath}");
        }

        if (options.VersionFilePath is not null && !File.Exists(options.VersionFilePath))
        {
            throw new FileNotFoundException($"Version file not found: {options.VersionFilePath}");
        }
    }

    private string ResolvePackageName()
    {
        if (!string.IsNullOrWhiteSpace(options.PackageName))
        {
            return options.PackageName!;
        }

        try
        {
            var document = XDocument.Load(options.ProjectFilePath);
            var packageId = document.Descendants().FirstOrDefault(x => string.Equals(x.Name.LocalName, "PackageId", StringComparison.OrdinalIgnoreCase))?.Value;

            if (!string.IsNullOrWhiteSpace(packageId))
            {
                return packageId.Trim();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Unable to parse PackageId from project file ({ex.Message}). Falling back to file name.");
        }

        return Path.GetFileNameWithoutExtension(options.ProjectFilePath);
    }

    private string ResolveVersion()
    {
        if (!string.IsNullOrWhiteSpace(options.VersionStatic))
        {
            return options.VersionStatic!;
        }

        var versionFile = options.VersionFilePath ?? options.ProjectFilePath;
        return VersionResolver.ExtractVersion(versionFile, options.VersionRegex, options.VersionRegexOptions);
    }

    private async Task<bool> PackageVersionIsMissingAsync(string packageName, string version)
    {
        try
        {
            var baseUrl = BuildFlatContainerBaseUrl();
            var requestUri = $"{baseUrl}/{packageName.ToLowerInvariant()}/index.json";
            using var response = await _httpClient.GetAsync(requestUri);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Logger.Info("Package not found on feed. Treating as first release.");
                return true;
            }

            response.EnsureSuccessStatusCode();
            await using var contentStream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(contentStream);
            if (document.RootElement.TryGetProperty("versions", out var versionsElement))
            {
                foreach (var element in versionsElement.EnumerateArray())
                {
                    if (string.Equals(element.GetString(), version, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to query NuGet feed for existing versions: {ex.Message}", ex);
        }
    }

    private string BuildFlatContainerBaseUrl()
    {
        var source = (options.NugetSource ?? "https://api.nuget.org").TrimEnd('/');
        const string indexSuffix = "/v3/index.json";
        if (source.EndsWith(indexSuffix, StringComparison.OrdinalIgnoreCase))
        {
            source = source[..^indexSuffix.Length].TrimEnd('/');
        }

        var suffix = "/v3-flatcontainer";
        if (source.EndsWith("v3-flatcontainer", StringComparison.OrdinalIgnoreCase))
        {
            return source;
        }

        return source + suffix;
    }

    private void PrepareOutputDirectory()
    {
        Directory.CreateDirectory(options.EffectiveOutputDirectory);
        foreach (var file in Directory.EnumerateFiles(options.EffectiveOutputDirectory, "*.nupkg", SearchOption.TopDirectoryOnly))
        {
            File.Delete(file);
        }

        foreach (var file in Directory.EnumerateFiles(options.EffectiveOutputDirectory, "*.snupkg", SearchOption.TopDirectoryOnly))
        {
            File.Delete(file);
        }
    }

    private async Task PackAsync(string packageName, string version)
    {
        var args = new List<string>
        {
            "pack",
            options.ProjectFilePath,
            "-c", options.Configuration,
            "-o", options.EffectiveOutputDirectory,
            $"-p:PackageVersion={version}"
        };

        if (options.ContinuousIntegrationBuild)
        {
            args.Add("-p:ContinuousIntegrationBuild=true");
        }

        if (options.NoRestore)
        {
            args.Add("--no-restore");
        }
        else
        {
            args.Add("-p:RestoreLockedMode=false");
        }

        if (!string.Equals(packageName, Path.GetFileNameWithoutExtension(options.ProjectFilePath), StringComparison.Ordinal))
        {
            args.Add($"-p:PackageId={packageName}");
        }

        if (options.IncludeSymbols)
        {
            args.Add("--include-symbols");
            args.Add("-p:SymbolPackageFormat=snupkg");
        }

        args.AddRange(ArgumentSplitter.Split(options.ExtraPackArguments));

        await ProcessRunner.RunAsync("dotnet", args, options.WorkingDirectory);
    }

    private string? LocatePackageFile(string packageName, string version, string extension)
    {
        var pattern = $"{packageName}.{version}*{extension}";
        var candidates = Directory.EnumerateFiles(options.EffectiveOutputDirectory, pattern, SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(options.EffectiveOutputDirectory, $"*{extension}", SearchOption.TopDirectoryOnly))
            .Where(path => string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetCreationTimeUtc)
            .ToList();

        return candidates.FirstOrDefault();
    }

    private async Task PushPackageAsync(string packagePath, string source, string apiKey, bool includeSymbols)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException($"Package file not found: {packagePath}");
        }

        List<string> args =
        [
            "nuget", "push",
            packagePath,
            "--source", source,
            "--api-key", apiKey,
            "--skip-duplicate"
        ];

        if (!includeSymbols)
        {
            args.Add("--no-symbols");
        }

        await ProcessRunner.RunAsync("dotnet", args, options.WorkingDirectory, [apiKey]);
    }

    private string? ResolveGitHubPackagesSource()
    {
        if (!string.IsNullOrWhiteSpace(options.GitHubPackagesSource))
        {
            return options.GitHubPackagesSource;
        }

        var owner = options.GitHubPackagesOwner;
        if (string.IsNullOrWhiteSpace(owner))
        {
            owner = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY_OWNER");
        }

        if (string.IsNullOrWhiteSpace(owner))
        {
            return null;
        }

        return $"https://nuget.pkg.github.com/{owner}/index.json";
    }
}
