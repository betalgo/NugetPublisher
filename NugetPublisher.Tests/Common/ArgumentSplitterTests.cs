using NugetPublisher.Common;
using Xunit;

namespace NugetPublisher.Tests.Common;

public class ArgumentSplitterTests
{
    [Fact]
    public void Split_ReturnsEmpty_ForNullOrWhitespace()
    {
        Assert.Empty(ArgumentSplitter.Split(null));
        Assert.Empty(ArgumentSplitter.Split("   "));
        Assert.Empty(ArgumentSplitter.Split(string.Empty));
    }

    [Fact]
    public void Split_SplitsOnWhitespace_WhenNotInQuotes()
    {
        var parts = ArgumentSplitter.Split("one two  three").ToArray();
        Assert.Equal(new[] { "one", "two", "three" }, parts);
    }

    [Fact]
    public void Split_KeepsQuotedSegments_Together_AndStripsQuotes()
    {
        var parts = ArgumentSplitter.Split("one \"two three\" four").ToArray();
        Assert.Equal(new[] { "one", "two three", "four" }, parts);
    }

    [Fact]
    public void Split_Handles_TrailingAndLeadingWhitespace()
    {
        var parts = ArgumentSplitter.Split("  \"hello world\"   ").ToArray();
        Assert.Equal(new[] { "hello world" }, parts);
    }
}
