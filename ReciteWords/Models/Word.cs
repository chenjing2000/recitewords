namespace ReciteWords.Models;

public class Word
{
    public string Wid { get; set; } = "";
    public string Text { get; set; } = "";
    public string Phonetic { get; set; } = "";
    public List<WordSense> Senses { get; set; } = new List<WordSense>();
    public string Notes { get; set; } = "";
    public string Etymology { get; set; } = "";
}
