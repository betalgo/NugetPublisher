using System.Text;

namespace NugetPublisher.Common;

internal static class ArgumentSplitter
{
    public static IEnumerable<string> Split(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        var builder = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"':
                    inQuotes = !inQuotes;
                    continue;
                default:
                    if (char.IsWhiteSpace(ch) && !inQuotes)
                    {
                        if (builder.Length > 0)
                        {
                            yield return builder.ToString();
                            builder.Clear();
                        }
                    }
                    else
                    {
                        builder.Append(ch);
                    }

                    break;
            }
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }
}
