using ReciteWords.Common;
using ReciteWords.Models;
namespace ReciteWords.Audio;

public class AudioCatalog : IAudioCatalog
{
    private string audioFolder = "";
    private string exampleFolder = "";
    private readonly HashSet<string> audioWords = new HashSet<string>(StringComparer.Ordinal);
    public void Load(string wordListPath, WordList words)
    {
        audioWords.Clear();
        string folder = Path.Combine(Path.GetDirectoryName(wordListPath)!, Path.GetFileNameWithoutExtension(wordListPath));
        audioFolder = Path.Combine(folder, "audio");
        exampleFolder = Path.Combine(folder, "examples");
        var stems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in words.Words)
        {
            // 首词按单词本顺序占用 stem，与文件是否存在或写入成功无关。
            if (stems.Add(AudioFileName.Stem(word.Text))) audioWords.Add(Spelling.Fold(word.Text));
        }
    }
    public string? FindWord(string word, string accent)
    {
        if (!audioWords.Contains(Spelling.Fold(word))) return null;
        return FindFile(audioFolder, AudioFileName.Stem(word), accent);
    }
    public string? FindExample(string word, string eid, string accent)
    {
        if (!audioWords.Contains(Spelling.Fold(word)) || eid.Length != 2 || !JsonFields.Digits(eid) || eid == "00") return null;
        return FindFile(exampleFolder, AudioFileName.Stem(word) + "_e" + eid, accent);
    }
    private static string? FindFile(string folder, string stem, string accent)
    {
        if (folder.Length == 0 || stem.Length == 0 || (accent != "uk" && accent != "us")) return null;
        foreach (string extension in new[] { ".mp3", ".wav" })
        {
            string path = Path.Combine(folder, stem + "_" + accent + extension);
            if (File.Exists(path)) return path;
        }
        return null;
    }
}
