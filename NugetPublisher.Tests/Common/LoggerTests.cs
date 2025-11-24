using NugetPublisher.Common;
using Xunit;

namespace NugetPublisher.Tests.Common;

public class LoggerTests
{
    [Fact]
    public void Info_Writes_Info_Line_To_StdOut()
    {
        var sw = new StringWriter();
        var original = Console.Out;
        try
        {
            Console.SetOut(sw);
            Logger.Info("hello");
            var text = sw.ToString();
            Assert.Contains("INFO: hello", text);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Fact]
    public void Warn_Writes_Github_Warning_And_Message()
    {
        var sw = new StringWriter();
        var original = Console.Out;
        try
        {
            Console.SetOut(sw);
            Logger.Warn("be careful");
            var text = sw.ToString();
            Assert.Contains("::warning::be careful", text);
            Assert.Contains("WARN: be careful", text);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Fact]
    public void Error_Writes_Github_Error_And_Message_To_StdErr()
    {
        var sw = new StringWriter();
        var original = Console.Error;
        try
        {
            Console.SetError(sw);
            Logger.Error("oops");
            var text = sw.ToString();
            Assert.Contains("::error::oops", text);
            Assert.Contains("ERROR: oops", text);
        }
        finally
        {
            Console.SetError(original);
        }
    }
}
