using ReciteWords.Models;
namespace ReciteWords.Storage;

public interface IWordListRepository
{
    ScanResult Scan(string folder);
    WordList Load(string path);
}
public class ScanResult
{
    public List<WordListEntry> Entries { get; } = new List<WordListEntry>();
    public List<string> Errors { get; } = new List<string>();
}
