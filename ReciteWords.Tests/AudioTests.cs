using ReciteWords.Audio;
using ReciteWords.Storage;
namespace ReciteWords.Tests;
internal static partial class Program
{
    static void LoadCatalog(AudioCatalog catalog, string path)
    {
        catalog.Load(path, new WordListRepository().Load(path));
    }
    static void AudioTests()
    {
        Check("direct word filenames and UK not replaced by US", () => {
            string path = Write("audio-list.json", Minimal); string folder = Path.Combine(Temp, "audio-list", "audio"); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "aware_us.mp3"), "fixture");
            var catalog = new AudioCatalog(); LoadCatalog(catalog, path);
            Equal<string?>(null, catalog.FindWord("aware", "uk")); Equal(true, string.Equals(Path.Combine(folder, "aware_us.mp3"), catalog.FindWord("AWARE", "us"), StringComparison.OrdinalIgnoreCase));
        });
        Check("example audio combines word stem and local eid", () => {
            string path = Write("example-audio.json", ExampleList("one's own", "second"));
            string folder = Path.Combine(Temp, "example-audio", "examples"); Directory.CreateDirectory(folder);
            string first = Path.Combine(folder, "one_s_own_e01_uk.mp3"); File.WriteAllText(first, "fixture");
            string second = Path.Combine(folder, "second_e01_uk.wav"); File.WriteAllText(second, "fixture");
            var catalog = new AudioCatalog(); LoadCatalog(catalog, path);
            Equal(first, catalog.FindExample("one's own", "01", "uk")); Equal(second, catalog.FindExample("second", "01", "uk"));
            Equal<string?>(null, catalog.FindExample("one's own", "001234", "uk")); Equal<string?>(null, catalog.FindExample("one's own", "00", "uk"));
        });
        Check("duplicate stem skips all later audio even if first files absent", () => {
            string path = Write("duplicate-audio.json", ExampleList("well-being", "well being"));
            string folder = Path.Combine(Temp, "duplicate-audio"); Directory.CreateDirectory(Path.Combine(folder, "audio")); Directory.CreateDirectory(Path.Combine(folder, "examples"));
            var catalog = new AudioCatalog(); LoadCatalog(catalog, path);
            Equal<string?>(null, catalog.FindWord("well-being", "uk")); Equal<string?>(null, catalog.FindWord("well being", "uk"));
            string wordFile = Path.Combine(folder, "audio", "well_being_uk.mp3"); File.WriteAllText(wordFile, "fixture");
            string exampleFile = Path.Combine(folder, "examples", "well_being_e01_uk.mp3"); File.WriteAllText(exampleFile, "fixture");
            Equal(wordFile, catalog.FindWord("well-being", "uk")); Equal(exampleFile, catalog.FindExample("well-being", "01", "uk"));
            Equal<string?>(null, catalog.FindWord("well being", "uk")); Equal<string?>(null, catalog.FindExample("well being", "01", "uk"));
            File.WriteAllText(path, ExampleList("well being", "well-being")); LoadCatalog(catalog, path);
            Equal(wordFile, catalog.FindWord("well being", "uk")); Equal<string?>(null, catalog.FindWord("well-being", "uk"));
        });
        Check("legacy indexes are ignored and missing files are disabled", () => {
            string path = Write("old-index.json", Minimal); string folder = Path.Combine(Temp, "old-index"); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "audio.json"), "{broken"); File.WriteAllText(Path.Combine(folder, "examples.json"), "{broken");
            var catalog = new AudioCatalog(); LoadCatalog(catalog, path);
            Equal<string?>(null, catalog.FindWord("aware", "uk")); Equal<string?>(null, catalog.FindExample("aware", "01", "uk"));
        });
    }
}
