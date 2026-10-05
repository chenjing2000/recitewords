namespace ReciteWords.Models;

public class ReviewProgress
{
    public string Application { get; set; } = "ReciteWords";
    public int SchemaVersion { get; set; } = 2;
    public Dictionary<string, StudyLevel> Words { get; set; } = new Dictionary<string, StudyLevel>(StringComparer.Ordinal);
}
