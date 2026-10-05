using ReciteWords.Models;
namespace ReciteWords.Audio;

public interface IAudioCatalog
{
    void Load(string wordListPath, WordList words);
    string? FindWord(string word, string accent);
    string? FindExample(string word, string eid, string accent);
}
