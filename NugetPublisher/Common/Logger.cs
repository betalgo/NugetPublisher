namespace NugetPublisher.Common;

internal static class Logger
{
    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);
    public static void Debug(string message) => Write("DEBUG", message);

    private static void Write(string level, string message)
    {
        var line = $"[{DateTime.UtcNow:O}] {level}: {message}";
        switch (level)
        {
            case "WARN":
                Console.WriteLine($"::warning::{message}");
                Console.WriteLine(line);
                break;
            case "ERROR":
                Console.Error.WriteLine($"::error::{message}");
                Console.Error.WriteLine(line);
                break;
            default:
                Console.WriteLine(line);
                break;
        }
    }
}
