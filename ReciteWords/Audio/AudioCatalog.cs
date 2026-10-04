using ReciteWords.Common;
using System.Text.Json;
namespace ReciteWords.Audio;

public class AudioCatalog : IAudioCatalog
{
    private readonly Dictionary<string, Dictionary<string, string>> words = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, string>> examples = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
    public AudioLoadResult Load(string wordListPath)
    {
        words.Clear(); examples.Clear();
        var result = new AudioLoadResult();
        string folder = Path.Combine(Path.GetDirectoryName(wordListPath)!, Path.GetFileNameWithoutExtension(wordListPath));
        LoadFile(folder, "audio.json", false, result); LoadFile(folder, "examples.json", true, result);
        return result;
    }
    private void LoadFile(string folder, string name, bool isExample, AudioLoadResult result)
    {
        string path = Path.Combine(folder, name); if (!File.Exists(path)) return;
        var target = isExample ? examples : words;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement; JsonFields.Object(root);
            if (!root.TryGetProperty("schema_version", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int number) || number != 1)
                throw new InvalidDataException("音频配置版本必须为 1");
            if (!root.TryGetProperty("words", out var entries)) throw new InvalidDataException("音频配置缺少 words");
            JsonFields.Object(entries);
            foreach (var item in entries.EnumerateObject())
            {
                if (isExample)
                {
                    if (item.Value.ValueKind != JsonValueKind.Array) throw new InvalidDataException("例句集合必须是数组: " + item.Name);
                    foreach (var entry in item.Value.EnumerateArray())
                    {
                        JsonFields.Object(entry);
                        string eid = JsonFields.Text(entry, "eid", true);
                        if (eid.Length != 6 || !JsonFields.Digits(eid) || target.ContainsKey(eid)) throw new InvalidDataException("例句 eid 必须是唯一的六位数字字符串");
                        var accents = new Dictionary<string, string>();
                        foreach (string accent in new[] { "uk", "us" })
                        {
                            string value = JsonFields.Text(entry, accent);
                            string? file = ExistingPath(folder, value); if (file != null) accents[accent] = file;
                        }
                        target.Add(eid, accents);
                    }
                }
                else
                {
                    JsonFields.Object(item.Value);
                    var accents = new Dictionary<string, string>();
                    foreach (string accent in new[] { "uk", "us" })
                        foreach (string value in JsonFields.Strings(item.Value, accent))
                        {
                            string? file = ExistingPath(folder, value);
                            if (file != null) { accents[accent] = file; break; }
                        }
                    string key = Spelling.Fold(item.Name);
                    if (!target.TryAdd(key, accents)) throw new InvalidDataException("重复音频拼写: " + item.Name);
                }
            }
        }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is JsonException || ex is UnauthorizedAccessException || ex is ArgumentException)
        { target.Clear(); result.Errors.Add(name + ": " + ex.Message); }
    }
    private static string? ExistingPath(string folder, string relative)
    {
        if (relative.Length == 0) return null;
        string path = Path.GetFullPath(Path.Combine(folder, relative));
        return File.Exists(path) ? path : null;
    }
    public string? FindWord(string word, string accent) => Find(words, Spelling.Fold(word), accent);
    public string? FindExample(string eid, string accent) => Find(examples, eid, accent);
    private static string? Find(Dictionary<string, Dictionary<string, string>> items, string id, string accent)
    {
        if (items.TryGetValue(id, out var accents) && accents.TryGetValue(accent, out var path) && File.Exists(path)) return path;
        return null;
    }
}
