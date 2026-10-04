namespace ReciteWords.Models;

public class WordSense
{
    public string Pos { get; set; } = "";
    public string EnglishMeaning { get; set; } = "";
    public string ChineseMeaning { get; set; } = "";
    public List<string> Register { get; set; } = new List<string>();
    public string Eid { get; set; } = "";
    public string Example { get; set; } = "";
    public string ExampleTranslation { get; set; } = "";
    public List<string> Synonyms { get; set; } = new List<string>();
    public List<string> Antonyms { get; set; } = new List<string>();
    public List<string> Collocations { get; set; } = new List<string>();
}
