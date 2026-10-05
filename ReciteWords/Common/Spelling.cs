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
        // 固定映射让词库重复检测和进度键使用相同的 Unicode casefold 规则。
        using var stream = typeof(Spelling).Assembly.GetManifestResourceStream("ReciteWords.Resources.CaseFolding.json")!;
        return JsonSerializer.Deserialize<Dictionary<int, string>>(stream)!;
    }
}
