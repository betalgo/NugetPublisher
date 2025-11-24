using NugetPublisher.Application;
using NugetPublisher.Cli;
using Xunit;

namespace NugetPublisher.Tests.Application.Publishing;

public class PublisherTests
{
    [Fact]
    public async Task ExecuteAsync_Writes_Outputs_And_Skips_When_DryRun_And_SkipCheck_Disabled()
    {
        var tempDir = CreateTempDir();
        var csprojPath = Path.Combine(tempDir, "MyProject.csproj");
        await File.WriteAllTextAsync(csprojPath, MinimalProjectXml("net8.0", "1.2.3", "MyProject"));

        var outputFile = Path.Combine(tempDir, "gh_output.txt");

        var origWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        var origProjectFile = Environment.GetEnvironmentVariable("PROJECT_FILE");
        var origOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        var origDryRun = Environment.GetEnvironmentVariable("DRY_RUN");
        var origSkip = Environment.GetEnvironmentVariable("SKIP_WHEN_VERSION_EXISTS");
        var origVersionStatic = Environment.GetEnvironmentVariable("VERSION_STATIC");

        try
        {
            Environment.SetEnvironmentVariable("GITHUB_WORKSPACE", tempDir);
            Environment.SetEnvironmentVariable("PROJECT_FILE", "MyProject.csproj");
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", outputFile);
            Environment.SetEnvironmentVariable("DRY_RUN", "true");
            Environment.SetEnvironmentVariable("SKIP_WHEN_VERSION_EXISTS", "false");
            Environment.SetEnvironmentVariable("VERSION_STATIC", "9.9.9");

            var options = Options.FromEnvironment();
            using var publisher = new Publisher(options);
            await publisher.ExecuteAsync();

            var text = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("package_name=MyProject", text);
            Assert.Contains("version=9.9.9", text);
            Assert.Contains("should_publish=true", text);
            Assert.Contains("published=false", text);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_WORKSPACE", origWorkspace);
            Environment.SetEnvironmentVariable("PROJECT_FILE", origProjectFile);
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", origOutput);
            Environment.SetEnvironmentVariable("DRY_RUN", origDryRun);
            Environment.SetEnvironmentVariable("SKIP_WHEN_VERSION_EXISTS", origSkip);
            Environment.SetEnvironmentVariable("VERSION_STATIC", origVersionStatic);

            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_Respects_PackageName_Override()
    {
        var tempDir = CreateTempDir();
        var csprojPath = Path.Combine(tempDir, "MyProject.csproj");
        await File.WriteAllTextAsync(csprojPath, MinimalProjectXml("net8.0", "1.0.0", "FromProject"));

        var outputFile = Path.Combine(tempDir, "gh_output.txt");

        var origWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        var origProjectFile = Environment.GetEnvironmentVariable("PROJECT_FILE");
        var origOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        var origDryRun = Environment.GetEnvironmentVariable("DRY_RUN");
        var origSkip = Environment.GetEnvironmentVariable("SKIP_WHEN_VERSION_EXISTS");
        var origVersionStatic = Environment.GetEnvironmentVariable("VERSION_STATIC");
        var origPackageName = Environment.GetEnvironmentVariable("PACKAGE_NAME");

        try
        {
            Environment.SetEnvironmentVariable("GITHUB_WORKSPACE", tempDir);
            Environment.SetEnvironmentVariable("PROJECT_FILE", "MyProject.csproj");
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", outputFile);
            Environment.SetEnvironmentVariable("DRY_RUN", "true");
            Environment.SetEnvironmentVariable("SKIP_WHEN_VERSION_EXISTS", "false");
            Environment.SetEnvironmentVariable("VERSION_STATIC", "1.0.0");
            Environment.SetEnvironmentVariable("PACKAGE_NAME", "OverrideName");

            var options = Options.FromEnvironment();
            using var publisher = new Publisher(options);
            await publisher.ExecuteAsync();

            var text = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("package_name=OverrideName", text);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_WORKSPACE", origWorkspace);
            Environment.SetEnvironmentVariable("PROJECT_FILE", origProjectFile);
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", origOutput);
            Environment.SetEnvironmentVariable("DRY_RUN", origDryRun);
            Environment.SetEnvironmentVariable("SKIP_WHEN_VERSION_EXISTS", origSkip);
            Environment.SetEnvironmentVariable("VERSION_STATIC", origVersionStatic);
            Environment.SetEnvironmentVariable("PACKAGE_NAME", origPackageName);

            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "np_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string MinimalProjectXml(string tfm, string version, string packageId) =>
        $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{tfm}</TargetFramework>
    <Version>{version}</Version>
    <PackageId>{packageId}</PackageId>
  </PropertyGroup>
</Project>";
}
