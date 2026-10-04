namespace ReciteWords.Models;

public class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public string Folder { get; set; } = "";
    public string WordListFile { get; set; } = "";
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
