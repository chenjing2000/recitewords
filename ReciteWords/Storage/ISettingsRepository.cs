using ReciteWords.Models;
namespace ReciteWords.Storage;

public interface ISettingsRepository
{
    AppSettings Load();
    void Save(AppSettings settings);
}
