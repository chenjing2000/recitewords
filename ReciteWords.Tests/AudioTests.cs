using ReciteWords.Audio;
namespace ReciteWords.Tests;
internal static partial class Program
{
    static void AudioTests()
    {
        Check("direct word filenames and UK not replaced by US", () => {
            string path = Write("audio-list.json", Minimal); string folder = Path.Combine(Temp, "audio-list", "audio"); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "aware_us.mp3"), "fixture");
            var catalog = new AudioCatalog(); Equal(0, catalog.Load(path).Errors.Count);
            Equal<string?>(null, catalog.FindWord("aware", "uk")); Equal(true, string.Equals(Path.Combine(folder, "aware_us.mp3"), catalog.FindWord("AWARE", "us"), StringComparison.OrdinalIgnoreCase));
        });
        Check("example uses exact eid with leading zero", () => {
            string path = Write("example-audio.json", Minimal); string folder = Path.Combine(Temp, "example-audio"); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "example uk.mp3"), "fixture");
            File.WriteAllText(Path.Combine(folder, "examples.json"), "{\"schema_version\":1,\"words\":{\"anything\":[{\"eid\":\"001234\",\"text\":\"different text\",\"uk\":\"example uk.mp3\"}]}}");
            var catalog = new AudioCatalog(); Equal(0, catalog.Load(path).Errors.Count);
            Equal(Path.Combine(folder, "example uk.mp3"), catalog.FindExample("001234", "uk")); Equal<string?>(null, catalog.FindExample("1234", "uk"));
        });
        Check("missing or corrupt audio does not block catalog", () => {
            var catalog = new AudioCatalog(); string path = Write("no-audio.json", Minimal); Equal(0, catalog.Load(path).Errors.Count);
            string folder = Path.Combine(Temp, "no-audio"); Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "audio.json"), "{}");
            Equal(0, catalog.Load(path).Errors.Count); Equal<string?>(null, catalog.FindWord("aware", "uk"));
        });
        Check("malformed example collection reports an error", () => {
            string path = Write("bad-examples.json", Minimal);
            string folder = Path.Combine(Temp, "bad-examples"); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "examples.json"), "{\"schema_version\":1,\"words\":{\"aware\":{\"eid\":\"001234\"}}}");
            var catalog = new AudioCatalog(); Equal(1, catalog.Load(path).Errors.Count);
            Equal<string?>(null, catalog.FindExample("001234", "uk"));
        });
    }
}
