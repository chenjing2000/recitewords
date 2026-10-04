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
        if (!File.Exists(file))
        {
            var initial = new ReviewProgress();
            foreach (var word in words.Words) initial.Session.Queue.Add(word.Wid);
            initial.Session.Completed = initial.Session.Queue.Count == 0;
            Save(path, initial);
            return initial;
        }
        string text = File.ReadAllText(file);
        using var document = JsonDocument.Parse(text);
        JsonElement root = document.RootElement;
        JsonFields.Object(root);
        if (JsonFields.Text(root, "application", true) != "ReciteWords" || !root.TryGetProperty("schema_version", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int number) || number != 1)
            throw new InvalidDataException("不是受支持的 ReciteWords 学习记录");
        if (!root.TryGetProperty("words", out var items) || !root.TryGetProperty("session", out var session)) throw new InvalidDataException("学习记录缺少 words 或 session");
        JsonFields.Object(items); JsonFields.Object(session);
        foreach (string key in new[] { "filter", "queue", "position", "completed" })
            if (!session.TryGetProperty(key, out _)) throw new InvalidDataException("学习记录缺少 session." + key);
        foreach (var property in items.EnumerateObject())
        {
            JsonFields.Object(property.Value);
            if (!property.Value.TryGetProperty("level", out _) || !property.Value.TryGetProperty("review_count", out _)) throw new InvalidDataException("词条记录缺少评价数据");
        }
        var result = JsonSerializer.Deserialize<ReviewProgress>(text, JsonFileWriter.Options) ?? throw new InvalidDataException("学习记录为空");
        Validate(result);
        return result;
    }
    public void Save(string path, ReviewProgress progress)
    {
        Validate(progress);
        JsonFileWriter.Write(ProgressPath(path), JsonSerializer.Serialize(progress, JsonFileWriter.Options));
    }
    private static void Validate(ReviewProgress progress)
    {
        if (progress.Application != "ReciteWords" || progress.SchemaVersion != 1 || progress.Words == null || progress.Session == null || progress.Session.Queue == null)
            throw new InvalidDataException("学习记录结构无效");
        foreach (var item in progress.Words)
            if (string.IsNullOrWhiteSpace(item.Key) || item.Value == null || !Enum.IsDefined(item.Value.Level) || item.Value.ReviewCount < 0)
                throw new InvalidDataException("词条学习记录无效");
        var state = progress.Session;
        if (!Enum.IsDefined(state.Filter) || state.Position < 0 || (state.Queue.Count == 0 && (state.Position != 0 || !state.Completed)) || (state.Queue.Count > 0 && state.Position >= state.Queue.Count) || (state.Completed && state.Queue.Count > 0 && state.Position != state.Queue.Count - 1))
            throw new InvalidDataException("复习队列位置无效");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in state.Queue)
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new InvalidDataException("复习队列 wid 无效或重复");
    }
}
