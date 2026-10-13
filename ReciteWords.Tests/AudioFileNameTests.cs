using ReciteWords.Audio;
using ReciteWords.Storage;
using System.Text.Json;
namespace ReciteWords.Tests;

internal static partial class Program
{
    static string PhraseList(params string[] phrases)
    {
        var words = new List<object>();
        foreach (string phrase in phrases)
            words.Add(new { word = phrase, phonetic_uk = "", phonetic_us = "", senses = new[] { new { pos = "phr.", chinese_meaning = "示例" } } });
        return JsonSerializer.Serialize(new { words });
    }
    static void AudioFileNameTests()
    {
        Check("unified ASCII stems collapse punctuation and lowercase", () => {
            foreach (var pair in new[] { (" One's Own ", "one_s_own"), ("A / B", "a_b"), ("_A!", "a"), ("café", "caf"), ("!!!", "") })
                Equal(pair.Item2, ReciteWords.Common.AudioFileName.Stem(pair.Item1));
        });
        Check("phrase and punctuation audio names use collapsed underscores", () => {
            string path = Write("phrases.json", PhraseList("take care of", "one's own", "well-being", "a / b", "_A!", "café"));
            string folder = Path.Combine(Temp, "phrases", "audio"); Directory.CreateDirectory(folder);
            var catalog = new AudioCatalog(); LoadCatalog(catalog, path);
            foreach (var pair in new[] { (" take care of ", "take_care_of"), ("one's own", "one_s_own"), ("well-being", "well_being"), ("a / b", "a_b"), ("_A!", "a"), ("café", "caf") })
            {
                string file = Path.Combine(folder, pair.Item2 + "_uk.mp3"); File.WriteAllText(file, "fixture");
                Equal(file, catalog.FindWord(pair.Item1, "uk"));
                string usFile = Path.Combine(folder, pair.Item2 + "_us.wav"); File.WriteAllText(usFile, "fixture");
                Equal(usFile, catalog.FindWord(pair.Item1, "us"));
            }
        });
        foreach (var pair in new[] { ("well-being", "well being"), ("one's own", "one_s_own"), ("A-B", "a b") })
            Check("normalized audio name collision remains readable: " + pair.Item1, () => {
                string path = Write("collision.json", PhraseList(pair.Item1, pair.Item2));
                Equal(2, new WordListRepository().Load(path).Words.Count);
            });
        Check("phrase spelling and progress key remain unchanged", () => {
            var words = new WordListRepository().Load(Write("phrase-state.json", PhraseList("one's own")));
            Equal("one's own", words.Words[0].Text);
            var session = new ReciteWords.Review.ReviewSession(); session.Load(words, new ReciteWords.Models.ReviewProgress());
            session.Rate(ReciteWords.Models.StudyLevel.Familiar);
            Equal(true, session.GetProgress().Words.ContainsKey("one's own"));
            Equal(false, session.GetProgress().Words.ContainsKey("one_s_own"));
        });
    }
}
