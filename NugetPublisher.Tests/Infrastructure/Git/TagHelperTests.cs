using NugetPublisher.Infrastructure.Git;
using Xunit;

namespace NugetPublisher.Tests.Infrastructure.Git;

public class TagHelperTests
{
    [Fact]
    public void FormatTagName_Replaces_Asterisk_With_Version()
    {
        var result = TagHelper.FormatTagName("v*", "1.2.3");
        Assert.Equal("v1.2.3", result);
    }

    [Fact]
    public void FormatTagName_Appends_When_No_Asterisk()
    {
        var result = TagHelper.FormatTagName("release-", "1.2.3");
        Assert.Equal("release-1.2.3", result);
    }

    [Fact]
    public void FormatTagName_Returns_Version_When_Template_Empty()
    {
        var result = TagHelper.FormatTagName("", "1.2.3");
        Assert.Equal("1.2.3", result);
    }
}
