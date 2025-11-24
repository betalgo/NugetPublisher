using System.Text.RegularExpressions;
using System.Xml.Linq;
using NugetPublisher.Common;

namespace NugetPublisher.Application;

internal static class VersionResolver
{
    public static string ExtractVersion(string filePath, string? regexPattern, RegexOptions options)
    {
        var content = File.ReadAllText(filePath);

        if (!string.IsNullOrWhiteSpace(regexPattern))
        {
            var match = Regex.Match(content, regexPattern, options);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }

            throw new InvalidOperationException($"Unable to extract version using regex '{regexPattern}'.");
        }

        try
        {
            var document = XDocument.Parse(content);
            var versionNode = document.Descendants().FirstOrDefault(x => string.Equals(x.Name.LocalName, "Version", StringComparison.OrdinalIgnoreCase)) ??
                              document.Descendants().FirstOrDefault(x => string.Equals(x.Name.LocalName, "PackageVersion", StringComparison.OrdinalIgnoreCase));

            if (versionNode is not null && !string.IsNullOrWhiteSpace(versionNode.Value))
            {
                return versionNode.Value.Trim();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to parse XML while locating the version ({ex.Message}). Falling back to default regex.");
        }

        const string fallbackPattern = @"<Version>\s*(?<value>.+?)\s*</Version>";
        var fallback = Regex.Match(content, fallbackPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (fallback.Success)
        {
            return fallback.Groups["value"].Value.Trim();
        }

        throw new InvalidOperationException($"Unable to determine version from '{filePath}'.");
    }
}
