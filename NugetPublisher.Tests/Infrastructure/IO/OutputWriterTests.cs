using NugetPublisher.Infrastructure.IO;
using Xunit;

namespace NugetPublisher.Tests.Infrastructure.IO;

public class OutputWriterTests
{
    [Fact]
    public void Set_Writes_NameEqualsValue_To_GithubOutput_File()
    {
        var temp = Path.GetTempFileName();
        var original = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        try
        {
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", temp);

            OutputWriter.Set("key", "value");

            var text = File.ReadAllText(temp);
            Assert.Contains("key=value", text);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", original);
            File.Delete(temp);
        }
    }

    [Fact]
    public void Set_Does_Nothing_When_Name_Or_Value_Invalid()
    {
        var temp = Path.GetTempFileName();
        var original = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        try
        {
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", temp);

            OutputWriter.Set("", "v");
            OutputWriter.Set("n", null);

            var text = File.ReadAllText(temp);
            Assert.True(string.IsNullOrEmpty(text));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", original);
            File.Delete(temp);
        }
    }
}
