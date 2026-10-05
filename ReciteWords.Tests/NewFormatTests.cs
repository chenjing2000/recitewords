using ReciteWords.Audio;
using ReciteWords.Storage;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace ReciteWords.Tests;

internal static partial class Program
{
    static void NewFormatTests()
    {
        const string json = "{\"words\":[{\"word\":\"loyalty\",\"phonetic_uk\":\"/uk/\",\"phonetic_us\":\"/us/\",\"senses\":[{\"pos\":\"n.\",\"chinese_meaning\":\"忠诚\"}]}]}";
        Check("new wordlist without wid accepts separate phonetics", () => {
            var words = new WordListRepository().Load(Write("new-format.json", json));
            Equal("loyalty", words.Words[0].Text);
            Equal("/uk/", words.Words[0].phonetic_uk); Equal("/us/", words.Words[0].phonetic_us);
        });
        foreach (string field in new[] { "phonetic_uk", "phonetic_us", "pos", "chinese_meaning", "word" })
        {
            string value = field == "pos" ? "n." : field == "chinese_meaning" ? "忠诚" : field == "word" ? "loyalty" : field == "phonetic_uk" ? "/uk/" : "/us/";
            Check("required field missing: " + field, () => {
                var root = JsonNode.Parse(json)!;
                var target = field == "pos" || field == "chinese_meaning" ? root["words"]![0]!["senses"]![0]!.AsObject() : root["words"]![0]!.AsObject();
                target.Remove(field);
                Reject(() => new WordListRepository().Load(Write("missing.json", root.ToJsonString())));
            });
            if (field == "pos" || field == "chinese_meaning" || field == "word")
                Check("required content empty: " + field, () => Reject(() => new WordListRepository().Load(Write("empty.json", json.Replace("\"" + value + "\"", "\" \"")))));
        }
        Check("empty phonetics allowed and obsolete fields ignored", () => {
            var word = new WordListRepository().Load(Write("empty-phonetics.json", json.Replace("/uk/", "").Replace("/us/", "").Replace("\"word\":", "\"wid\":42,\"phonetic\":false,\"word\":"))).Words[0];
            Equal("", word.phonetic_uk); Equal("", word.phonetic_us);
        });
        Check("duplicate spelling rejected without wid", () => {
            using var document = JsonDocument.Parse(json);
            string item = document.RootElement.GetProperty("words")[0].GetRawText();
            Reject(() => new WordListRepository().Load(Write("duplicates.json", "{\"words\":[" + item + "," + item.Replace("loyalty", "LOYALTY") + "]}")));
        });
        Check("legacy single phonetic is rejected", () => Reject(() => new WordListRepository().Load(Write("legacy.json", "{\"words\":[{\"wid\":\"loyalty\",\"word\":\"loyalty\",\"phonetic\":\"/old/\",\"senses\":[{\"pos\":\"n.\",\"chinese_meaning\":\"忠诚\"}]}]}"))));
        Check("new progress version accepts status strings", () => {
            string path = Write("new-progress.json", json);
            string file = ProgressRepository.ProgressPath(path); Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "{\"application\":\"ReciteWords\",\"schema_version\":2,\"words\":{\"loyalty\":\"Familiar\"}}");
            var repository = new ProgressRepository();
            var progress = repository.LoadOrCreate(path); repository.Save(path, progress);
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            Equal("Familiar", document.RootElement.GetProperty("words").GetProperty("loyalty").GetString());
            Equal(false, document.RootElement.TryGetProperty("session", out _));
        });
        Check("word audio is found without audio index", () => {
            string path = Write("direct-audio.json", json);
            string folder = Path.Combine(Temp, "direct-audio", "audio"); Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, "loyalty_uk.mp3"); File.WriteAllText(file, "fixture");
            var catalog = new AudioCatalog(); LoadCatalog(catalog, path);
            Equal(file, catalog.FindWord("loyalty", "uk")); Equal<string?>(null, catalog.FindWord("loyalty", "us"));
        });
        Check("word audio prefers mp3 then wav and ignores old index", () => {
            string path = Write("audio-names.json", json);
            string folder = Path.Combine(Temp, "audio-names", "audio"); Directory.CreateDirectory(folder);
            string wav = Path.Combine(folder, "loyalty_us.wav"); File.WriteAllText(wav, "fixture");
            var catalog = new AudioCatalog(); LoadCatalog(catalog, path); Equal(wav, catalog.FindWord("loyalty", "us"));
            string mp3 = Path.Combine(folder, "loyalty_us.mp3"); File.WriteAllText(mp3, "fixture"); Equal(mp3, catalog.FindWord("loyalty", "us"));
            File.WriteAllText(Path.Combine(Temp, "audio-names", "audio.json"), "{\"schema_version\":1,\"words\":{\"loyalty\":{\"uk\":[\"audio/loyalty_us.mp3\"]}}}");
            LoadCatalog(catalog, path); Equal<string?>(null, catalog.FindWord("loyalty", "uk"));
            Equal<string?>(null, catalog.FindWord("../loyalty", "us")); Equal<string?>(null, catalog.FindWord("loyalty", "other"));
            LoadCatalog(catalog, Write("another-list.json", json)); Equal<string?>(null, catalog.FindWord("loyalty", "us"));
        });
    }
}
