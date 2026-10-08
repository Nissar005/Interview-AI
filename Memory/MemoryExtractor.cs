using System.Text.RegularExpressions;

namespace PrivateAIOverlay.Memory;

public static partial class MemoryExtractor
{
    public static bool TryExtractMemoryInstruction(string text, out string memoryText, out string action)
    {
        memoryText = string.Empty;
        action = "none";

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (RememberRegex().IsMatch(text))
        {
            memoryText = StripCommand(text, "remember", "memorize", "save this", "keep this in mind", "from now on", "don't forget");
            action = "store";
            return !string.IsNullOrWhiteSpace(memoryText);
        }

        if (ForgetRegex().IsMatch(text))
        {
            memoryText = StripCommand(text, "forget that", "forget", "delete memory", "remove that");
            action = "delete";
            return !string.IsNullOrWhiteSpace(memoryText);
        }

        return false;
    }

    private static string StripCommand(string input, params string[] phrases)
    {
        var normalized = input.Trim();
        foreach (var phrase in phrases)
        {
            if (normalized.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                var index = normalized.IndexOf(phrase, StringComparison.OrdinalIgnoreCase);
                normalized = normalized[(index + phrase.Length)..].Trim();
                return normalized.Trim(' ', '.', '!', '?', ':', ';', '"', '\'');
            }
        }

        return normalized.Trim(' ', '.', '!', '?', ':', ';', '"', '\'');
    }

    [GeneratedRegex("remember|memorize|save this|keep this in mind|from now on|don't forget", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RememberRegex();

    [GeneratedRegex("forget that|forget|delete memory|remove that", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForgetRegex();
}
