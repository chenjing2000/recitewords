using System.Text;
using System.Text.Json;
namespace ReciteWords.Common;

public static class Spelling
{
    private static readonly Dictionary<int, string> caseFolding = LoadCaseFolding();
    public static string Fold(string text)
    {
        var result = new StringBuilder();
        foreach (var rune in text.Trim().EnumerateRunes())
            result.Append(caseFolding.TryGetValue(rune.Value, out var folded) ? folded : rune.ToString());
        return result.ToString();
    }
    private static Dictionary<int, string> LoadCaseFolding()
    {
        // Frozen Unicode data preserves the Python input format's full casefold rule.
        using var stream = typeof(Spelling).Assembly.GetManifestResourceStream("ReciteWords.Resources.CaseFolding.json")!;
        return JsonSerializer.Deserialize<Dictionary<int, string>>(stream)!;
    }
}
