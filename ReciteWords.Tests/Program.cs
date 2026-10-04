using ReciteWords.Storage;
using ReciteWords.Common;
using System.Text.Json;
namespace ReciteWords.Tests;
internal static partial class Program
{
    private static int passed;
    private static int failed;
    public static string Temp = "";
    [STAThread]
    static int Main(string[] args)
    {
        Temp = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../artifacts/tests/", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(Temp);
        try
        {
            WordListTests(); ProgressTests(); ReviewTests(); AudioTests(); ViewModelTests(); WindowTests();
            if (args.Contains("--ui-qa")) UiTests();
            int audioArgument = Array.IndexOf(args, "--mp3");
            if (audioArgument >= 0 && audioArgument + 1 < args.Length)
                Check("local MP3 decoding and playback completion", () => VerifyMedia(args[audioArgument + 1]));
        }
        finally { Directory.Delete(Temp, true); }
        Console.WriteLine($"Tests: {passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }
    public static void Check(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex); }
    }
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}");
    }
    public static void Reject(Action action)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        catch (JsonException) { return; }
        throw new Exception("Invalid input was accepted");
    }
    public static string Write(string name, string content)
    {
        string path = Path.Combine(Temp, name);
        File.WriteAllText(path, content);
        return path;
    }
    private const string Minimal = "{\"words\":[{\"wid\":\"a\",\"word\":\"aware\",\"phonetic\":\"/əˈweə/\",\"senses\":[{\"chinese_meaning\":\"知道的\"}]}]}";
    static void WordListTests()
    {
        var repo = new WordListRepository();
        Check("minimal wordlist and filename name", () => {
            var list = repo.Load(Write("minimal.json", Minimal));
            Equal("minimal", list.Name); Equal("a", list.Words[0].Wid);
        });
        Check("unknown nested fields ignored", () => {
            var list = repo.Load(Write("unknown.json", Minimal.Replace("\"words\":", "\"future\":{\"nested\":[null,42]},\"words\":").Replace("\"wid\":", "\"extra\":false,\"wid\":")));
            Equal(1, list.Words.Count);
        });
        Check("new sense fields and word etymology", () => {
            string json = Minimal.Replace("\"phonetic\":", "\"etymology\":\"词源\",\"phonetic\":").Replace("\"chinese_meaning\":", "\"register\":[\"formal\"],\"antonyms\":[\"unaware\"],\"example\":\"Example\",\"chinese_meaning\":");
            var word = repo.Load(Write("extended.json", json)).Words[0];
            Equal("词源", word.Etymology); Equal("formal", word.Senses[0].Register[0]); Equal("unaware", word.Senses[0].Antonyms[0]);
        });
        Check("arbitrary positive version preserved", () => {
            var list = repo.Load(Write("version.json", Minimal.Replace("{\"words\":", "{\"schema_version\":123456789012345678901234567890,\"words\":")));
            Equal("123456789012345678901234567890", list.SchemaVersion!.Value.ToString());
        });
        foreach (string value in new[] { "0", "-1", "1.0", "1e2", "true", "null", "\"2\"" })
            Check("invalid version " + value, () => Reject(() => repo.Load(Write("bad.json", Minimal.Replace("{\"words\":", "{\"schema_version\":" + value + ",\"words\":")))));
        Check("phonetic required", () => Reject(() => repo.Load(Write("bad.json", Minimal.Replace("\"phonetic\":\"/əˈweə/\",", "")))));
        Check("known optional wrong type rejected", () => Reject(() => repo.Load(Write("bad.json", Minimal.Replace("\"wid\":", "\"notes\":42,\"wid\":")))));
        Check("duplicate keys rejected", () => Reject(() => repo.Load(Write("bad.json", Minimal.Replace("\"wid\":", "\"wid\":\"x\",\"wid\":")))));
        Check("invalid Unicode text is rejected as an input error", () => Reject(() => repo.Load(Write("unicode.json", Minimal.Replace("aware", "\\ud800")))));
        Check("invalid Unicode array value is rejected", () => Reject(() => repo.Load(Write("unicode-array.json", Minimal.Replace("\"wid\":", "\"register\":[\"\\ud800\"],\"wid\":").Replace("\"chinese_meaning\":", "\"synonyms\":[\"\\ud800\"],\"chinese_meaning\":")))));
        Check("invalid Unicode field name is rejected", () => Reject(() => repo.Load(Write("unicode-name.json", Minimal.Replace("\"words\":", "\"\\ud800\":42,\"words\":")))));
        Check("example without eid allowed", () => Equal("Test", repo.Load(Write("example.json", Minimal.Replace("\"chinese_meaning\":", "\"example\":\"Test\",\"chinese_meaning\":"))).Words[0].Senses[0].Example));
        Check("eid leading zero retained", () => Equal("001234", repo.Load(Write("eid.json", Minimal.Replace("\"chinese_meaning\":", "\"eid\":\"001234\",\"chinese_meaning\":"))).Words[0].Senses[0].Eid));
        Check("scan isolates invalid and ignores subfolders", () => {
            var folder = Path.Combine(Temp, "scan"); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "ok.json"), Minimal); File.WriteAllText(Path.Combine(folder, "bad.json"), "{}");
            File.WriteAllText(Path.Combine(folder, "unicode.json"), Minimal.Replace("aware", "\\ud800"));
            Directory.CreateDirectory(Path.Combine(folder, "userdata")); File.WriteAllText(Path.Combine(folder, "userdata", "x.json"), Minimal);
            var result = repo.Scan(folder); Equal(1, result.Entries.Count); Equal(2, result.Errors.Count); Equal("ok", result.Entries[0].Name);
        });
        foreach (var pair in new[] { ("ﬅ", "st"), ("ſ", "s"), ("µ", "μ"), ("ŉ", "ʼn"), ("İ", "i̇"), ("Straße", "strasse") })
            Check("Unicode folding " + pair.Item1, () => Equal(pair.Item2, Spelling.Fold(pair.Item1)));
    }
}
