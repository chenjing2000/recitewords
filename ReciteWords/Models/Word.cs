namespace ReciteWords.Models;

public class Word
{
    public string Text { get; set; } = "";
    public string phonetic_uk { get; set; } = "";
    public string phonetic_us { get; set; } = "";
    public List<WordSense> Senses { get; set; } = new List<WordSense>();
    public string Notes { get; set; } = "";
    public string Etymology { get; set; } = "";
}
