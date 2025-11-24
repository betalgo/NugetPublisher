using System.Text;

namespace NugetPublisher.Infrastructure.IO;

internal static class OutputWriter
{
    public static void Set(string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(name) || value is null)
        {
            return;
        }

        var outputFile = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        if (string.IsNullOrWhiteSpace(outputFile))
        {
            return;
        }

        var line = $"{name}={value}{Environment.NewLine}";
        File.AppendAllText(outputFile, line, Encoding.UTF8);
    }
}
