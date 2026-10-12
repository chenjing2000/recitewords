using ReciteWords.Platform;
namespace ReciteWords.Tests;
internal static partial class Program
{
    static void WindowTests()
    {
        Check("missing settings are initialized on disk", () => {
            string path = Path.Combine(Temp, "Fresh.settings.json");
            var repository = new ReciteWords.Storage.SettingsRepository(path);
            var settings = repository.Load();
            Equal(true, File.Exists(path));
            Equal(1, repository.Load().SchemaVersion);
            Equal("", settings.Folder);
        });
        Check("existing settings survive loading", () => {
            string path = Path.Combine(Temp, "Existing.settings.json");
            var repository = new ReciteWords.Storage.SettingsRepository(path);
            repository.Save(new ReciteWords.Models.AppSettings { Folder = "chosen", Width = 1700 });
            string before = File.ReadAllText(path);
            Equal("chosen", repository.Load().Folder);
            Equal(1700.0, repository.Load().Width);
            Equal(before, File.ReadAllText(path));
        });
        Check("default initial size fits working area at 100 percent", () => {
            var size = WindowBounds.CalculateInitialSize(1920, 1040, 96); Equal(700.0, size.Width); Equal(850.0, size.Height);
        });
        Check("default initial size fits working area at 150 percent", () => {
            var size = WindowBounds.CalculateInitialSize(1920, 1040, 144); Equal(700.0, size.Width); Equal(true, Math.Abs(size.Height - 693.33333333333) < 0.001);
        });
    }
}
