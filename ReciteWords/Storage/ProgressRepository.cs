using ReciteWords.Common;
using ReciteWords.Models;
using System.Text.Json;
namespace ReciteWords.Storage;

public class ProgressRepository : IProgressRepository
{
    public static string ProgressPath(string path) => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, "userdata", Path.GetFileNameWithoutExtension(path) + ".progress.json");
    public ReviewProgress LoadOrCreate(string path, WordList words)
    {
        string file = ProgressPath(path);
        if (!File.Exists(file)) { var initial = new ReviewProgress(); Save(path, initial); return initial; }
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        JsonElement root = document.RootElement; JsonFields.Object(root);
        if (JsonFields.Text(root, "application", true) != "ReciteWords" || !root.TryGetProperty("schema_version", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int number) || number != 2)
            throw new InvalidDataException("只支持版本 2 的 ReciteWords 学习记录，旧格式不兼容");
        if (!root.TryGetProperty("words", out var items)) throw new InvalidDataException("学习记录缺少 words");
        foreach (var property in root.EnumerateObject())
            if (property.Name != "application" && property.Name != "schema_version" && property.Name != "words")
                throw new InvalidDataException("学习记录不支持字段: " + property.Name);
        JsonFields.Object(items);
        var result = new ReviewProgress();
        foreach (var property in items.EnumerateObject())
        {
            string text = JsonFields.Text(items, property.Name, true);
            StudyLevel level;
            if (text == "Unknown") level = StudyLevel.Unknown;
            else if (text == "Familiar") level = StudyLevel.Familiar;
            else if (text == "Mastered") level = StudyLevel.Mastered;
            else throw new InvalidDataException("无效学习状态: " + text);
            result.Words.Add(property.Name, level);
        }
        Validate(result); return result;
    }
    public void Save(string path, ReviewProgress progress)
    {
        Validate(progress);
        JsonFileWriter.Write(ProgressPath(path), JsonSerializer.Serialize(progress, JsonFileWriter.Options));
    }
    private static void Validate(ReviewProgress progress)
    {
        if (progress.Application != "ReciteWords" || progress.SchemaVersion != 2 || progress.Words == null)
            throw new InvalidDataException("学习记录结构无效");
        foreach (var item in progress.Words)
            if (string.IsNullOrWhiteSpace(item.Key) || item.Key != Spelling.Fold(item.Key.Trim()) || !Enum.IsDefined(item.Value))
                throw new InvalidDataException("词条学习记录必须使用标准化拼写和有效状态");
    }
}
