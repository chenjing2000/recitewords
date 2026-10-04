namespace ReciteWords.Audio;

public interface IAudioCatalog
{
    AudioLoadResult Load(string wordListPath);
    string? FindWord(string word, string accent);
    string? FindExample(string eid, string accent);
}
public class AudioLoadResult
{
    public List<string> Errors { get; } = new List<string>();
}
