using ReciteWords.Models;
using System.Text.Json;
namespace ReciteWords.Storage;

public class SettingsRepository : ISettingsRepository
{
    private readonly string path;
    public SettingsRepository(string path) { this.path = path; }
    public AppSettings Load()
    {
        if (!File.Exists(path)) return new AppSettings();
        var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonFileWriter.Options) ?? throw new InvalidDataException("设置文件为空");
        if (settings.SchemaVersion != 1 || settings.Folder == null || settings.WordListFile == null)
            throw new InvalidDataException("设置版本或结构无效");
        return settings;
    }
    public void Save(AppSettings settings) { JsonFileWriter.Write(path, JsonSerializer.Serialize(settings, JsonFileWriter.Options)); }
}
