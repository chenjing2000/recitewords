using ReciteWords.Common;
using System.Text.Json;
namespace ReciteWords.Audio;

public class AudioCatalog : IAudioCatalog
{
    private string audioFolder = "";
    private readonly Dictionary<string, Dictionary<string, string>> examples = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
    public AudioLoadResult Load(string wordListPath)
    {
        examples.Clear();
        var result = new AudioLoadResult();
        string folder = Path.Combine(Path.GetDirectoryName(wordListPath)!, Path.GetFileNameWithoutExtension(wordListPath));
        audioFolder = Path.Combine(folder, "audio");
        string path = Path.Combine(folder, "examples.json");
        if (!File.Exists(path)) return result;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement; JsonFields.Object(root);
            if (!root.TryGetProperty("schema_version", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int number) || number != 1)
                throw new InvalidDataException("例句音频配置版本必须为 1");
            if (!root.TryGetProperty("words", out var entries)) throw new InvalidDataException("例句音频配置缺少 words");
            JsonFields.Object(entries);
            foreach (var item in entries.EnumerateObject())
            {
                if (item.Value.ValueKind != JsonValueKind.Array) throw new InvalidDataException("例句集合必须是数组: " + item.Name);
                foreach (var entry in item.Value.EnumerateArray())
                {
                    JsonFields.Object(entry);
                    string eid = JsonFields.Text(entry, "eid", true);
                    if (eid.Length != 6 || !JsonFields.Digits(eid) || examples.ContainsKey(eid)) throw new InvalidDataException("例句 eid 必须是唯一的六位数字字符串");
                    var accents = new Dictionary<string, string>();
                    foreach (string accent in new[] { "uk", "us" })
                    {
                        string relative = JsonFields.Text(entry, accent);
                        if (relative.Length == 0) continue;
                        string file = Path.GetFullPath(Path.Combine(folder, relative));
                        if (File.Exists(file)) accents[accent] = file;
                    }
                    examples.Add(eid, accents);
                }
            }
        }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is JsonException || ex is UnauthorizedAccessException || ex is ArgumentException)
        { examples.Clear(); result.Errors.Add("examples.json: " + ex.Message); }
        return result;
    }
    public string? FindWord(string word, string accent)
    {
        if (audioFolder.Length == 0 || (accent != "uk" && accent != "us")) return null;
        string text = AudioFileName.Stem(word);
        if (text.Length == 0) return null;
        foreach (string extension in new[] { ".mp3", ".wav" })
        {
            string path = Path.Combine(audioFolder, text + "_" + accent + extension);
            if (File.Exists(path)) return path;
        }
        return null;
    }
    public string? FindExample(string eid, string accent)
    {
        if (examples.TryGetValue(eid, out var accents) && accents.TryGetValue(accent, out var path) && File.Exists(path)) return path;
        return null;
    }
}
