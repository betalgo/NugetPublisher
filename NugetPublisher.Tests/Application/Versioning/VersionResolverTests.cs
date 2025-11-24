using System.Text.RegularExpressions;
using NugetPublisher.Application;
using Xunit;

namespace NugetPublisher.Tests.Application.Versioning;

public class VersionResolverTests
{
    [Fact]
    public void ExtractVersion_Reads_Version_Node_From_Xml()
    {
        var xml = @"<Project><PropertyGroup><Version>1.2.3</Version></PropertyGroup></Project>";
        var path = WriteTempFile(xml);
        var version = VersionResolver.ExtractVersion(path, null, RegexOptions.None);
        Assert.Equal("1.2.3", version);
    }

    [Fact]
    public void ExtractVersion_Reads_PackageVersion_Node_From_Xml()
    {
        var xml = @"<Project><PropertyGroup><PackageVersion>2.0.0</PackageVersion></PropertyGroup></Project>";
        var path = WriteTempFile(xml);
        var version = VersionResolver.ExtractVersion(path, null, RegexOptions.None);
        Assert.Equal("2.0.0", version);
    }

    [Fact]
    public void ExtractVersion_Uses_Regex_With_CaptureGroup_When_Provided()
    {
        var content = "version: 3.4.5-build";
        var path = WriteTempFile(content);
        var version = VersionResolver.ExtractVersion(path, @"version:\s*([0-9]+\.[0-9]+\.[0-9]+)", RegexOptions.IgnoreCase);
        Assert.Equal("3.4.5", version);
    }

    [Fact]
    public void ExtractVersion_Throws_When_Regex_Does_Not_Match()
    {
        var content = "no version here";
        var path = WriteTempFile(content);
        Assert.Throws<InvalidOperationException>(() => VersionResolver.ExtractVersion(path, @"version:\s*(\S+)", RegexOptions.IgnoreCase));
    }

    [Fact]
    public void ExtractVersion_Falls_Back_To_Simple_Version_Tag_When_Xml_Parse_Fails()
    {
        var content = "<notXml><Version>9.9.9</Version></notXml>";
        var path = WriteTempFile(content);
        var version = VersionResolver.ExtractVersion(path, null, RegexOptions.None);
        Assert.Equal("9.9.9", version);
    }

    [Fact]
    public void ExtractVersion_Throws_When_No_Version_Found()
    {
        var content = "nothing useful";
        var path = WriteTempFile(content);
        Assert.Throws<InvalidOperationException>(() => VersionResolver.ExtractVersion(path, null, RegexOptions.None));
    }

    private static string WriteTempFile(string content)
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(file, content);
        return file;
    }
}
