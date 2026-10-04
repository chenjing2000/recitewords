namespace ReciteWords.Models;

public class ReviewProgress
{
    public string Application { get; set; } = "ReciteWords";
    public int SchemaVersion { get; set; } = 1;
    public Dictionary<string, WordProgress> Words { get; set; } = new Dictionary<string, WordProgress>(StringComparer.Ordinal);
    public ReviewSessionState Session { get; set; } = new ReviewSessionState();
}
public class ReviewSessionState
{
    public ReviewFilter Filter { get; set; } = ReviewFilter.All;
    public List<string> Queue { get; set; } = new List<string>();
    public int Position { get; set; }
    public bool Completed { get; set; }
}
