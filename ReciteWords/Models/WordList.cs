using System.Numerics;
namespace ReciteWords.Models;

public class WordList
{
    public BigInteger? SchemaVersion { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<Word> Words { get; set; } = new List<Word>();
}
public class WordListEntry
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
}
