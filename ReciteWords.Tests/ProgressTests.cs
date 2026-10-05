using ReciteWords.Models;
using ReciteWords.Storage;
using ReciteWords.Review;
using System.Text.Json;
namespace ReciteWords.Tests;
internal static partial class Program
{
    static WordList ThreeWords()
    {
        var result = new WordList { Name = "Sample" };
        foreach (string text in new[] { "a", "b", "c" }) result.Words.Add(new Word { Text = text, Senses = new List<WordSense>() { new WordSense { Pos = "n.", ChineseMeaning = text } } });
        return result;
    }
    static void ProgressTests()
    {
        var repository = new ProgressRepository();
        Check("userdata initialization and Python untouched", () => {
            string path = Write("education.json", Minimal);
            var python = Path.Combine(Temp, "education"); Directory.CreateDirectory(python);
            File.WriteAllText(Path.Combine(python, "progress.json"), "python untouched");
            var progress = repository.LoadOrCreate(path);
            Equal(true, File.Exists(ProgressRepository.ProgressPath(path))); Equal(0, progress.Words.Count);
            Equal("python untouched", File.ReadAllText(Path.Combine(python, "progress.json")));
        });
        Check("save and reopen only restores current status", () => {
            string path = Write("saved.json", Minimal);
            var progress = repository.LoadOrCreate(path); progress.Words["a"] = StudyLevel.Mastered; repository.Save(path, progress);
            Equal(StudyLevel.Mastered, repository.LoadOrCreate(path).Words["a"]);
            using var document = JsonDocument.Parse(File.ReadAllText(ProgressRepository.ProgressPath(path)));
            Equal(3, document.RootElement.EnumerateObject().Count());
            Equal("Mastered", document.RootElement.GetProperty("words").GetProperty("a").GetString());
            Equal(false, document.RootElement.TryGetProperty("session", out _));
        });
        Check("different wordlists isolated and rename creates", () => {
            string path = Write("first.json", Minimal); var first = repository.LoadOrCreate(path);
            first.Words["a"] = StudyLevel.Unknown; repository.Save(path, first);
            Equal(0, repository.LoadOrCreate(Write("renamed.json", Minimal)).Words.Count);
            Equal(1, repository.LoadOrCreate(path).Words.Count);
        });
        Check("corrupt progress never overwritten", () => {
            string path = Write("corrupt.json", Minimal); string file = ProgressRepository.ProgressPath(path);
            File.WriteAllText(file, "{broken"); Reject(() => repository.LoadOrCreate(path)); Equal("{broken", File.ReadAllText(file));
        });
        foreach (string text in new[] {
            "{\"application\":\"ReciteWords\",\"schema_version\":1,\"words\":{},\"session\":{}}",
            "{\"application\":\"ReciteWords\",\"schema_version\":2,\"words\":{\"a\":{\"level\":\"Unknown\",\"review_count\":1}}}",
            "{\"application\":\"ReciteWords\",\"schema_version\":2,\"words\":{\"a\":\"unknown\"}}",
            "{\"application\":\"ReciteWords\",\"schema_version\":2,\"words\":{\"A\":\"Unknown\"}}",
            "{\"application\":\"ReciteWords\",\"schema_version\":2,\"words\":{},\"position\":1}",
            "{\"application\":\"ReciteWords\",\"schema_version\":2,\"words\":{\"a\":0}}"
        })
            Check("invalid or legacy progress is not overwritten: " + text, () => {
                string path = Write("invalid-progress.json", Minimal); string file = ProgressRepository.ProgressPath(path);
                File.WriteAllText(file, text); Reject(() => repository.LoadOrCreate(path)); Equal(text, File.ReadAllText(file));
            });
        Check("failed atomic save leaves original bytes", () => {
            string path = Write("locked.json", Minimal); var progress = repository.LoadOrCreate(path); string file = ProgressRepository.ProgressPath(path);
            string before = File.ReadAllText(file);
            using (var handle = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bool failedSave = false; try { progress.Words["a"] = StudyLevel.Unknown; repository.Save(path, progress); } catch (IOException) { failedSave = true; }
                Equal(true, failedSave);
            }
            Equal(before, File.ReadAllText(file));
        });
    }
    static void ReviewTests()
    {
        Check("rating, navigation and rerating", () => {
            var session = new ReviewSession(); session.Load(ThreeWords(), new ReviewProgress());
            session.Rate(StudyLevel.Mastered); Equal("b", session.CurrentWord!.Text);
            session.Move(-1); session.Rate(StudyLevel.Unknown);
            Equal(StudyLevel.Unknown, session.GetProgress().Words["a"]); Equal(1, session.GetProgress().Words.Count); Equal("b", session.CurrentWord!.Text);
        });
        Check("filtered snapshot survives status changes", () => {
            var session = new ReviewSession(); session.Load(ThreeWords(), new ReviewProgress()); session.Start(ReviewFilter.Unseen);
            session.Rate(StudyLevel.Familiar); Equal(3, session.Count); session.Move(-1); Equal("a", session.CurrentWord!.Text); Equal(2, session.GetCount(ReviewFilter.Unseen));
        });
        Check("completion back to last and repeat", () => {
            var session = new ReviewSession(); session.Load(ThreeWords(), new ReviewProgress()); session.Move(1); session.Move(1);
            Equal("c", session.CurrentWord!.Text); session.Rate(StudyLevel.Mastered); Equal(true, session.Completed);
            Equal(true, session.Move(-1)); Equal("c", session.CurrentWord!.Text); session.Rate(StudyLevel.Unknown); Equal(true, session.Completed);
        });
        Check("deep snapshot rollback preserves queue and position", () => {
            var session = new ReviewSession(); session.Load(ThreeWords(), new ReviewProgress()); session.Start(ReviewFilter.Unseen); session.Move(1);
            var snapshot = session.Capture(); session.Rate(StudyLevel.Familiar); Equal(0, snapshot.Progress.Words.Count);
            session.Restore(snapshot); Equal("b", session.CurrentWord!.Text); Equal(ReviewFilter.Unseen, session.Filter); Equal(0, session.GetProgress().Words.Count);
        });
        Check("reopen starts first word and preserves status after reorder", () => {
            var session = new ReviewSession(); var words = ThreeWords(); session.Load(words, new ReviewProgress()); session.Rate(StudyLevel.Familiar);
            words.Words.Reverse(); session.Load(words, session.GetProgress());
            Equal("c", session.CurrentWord!.Text); Equal(ReviewFilter.All, session.Filter); Equal(StudyLevel.Familiar, session.GetProgress().Words["a"]);
        });
        Check("casefold spelling links progress without wid", () => {
            var words = ThreeWords(); words.Words[0].Text = "Straße";
            var session = new ReviewSession(); session.Load(words, new ReviewProgress()); session.Rate(StudyLevel.Mastered);
            Equal(StudyLevel.Mastered, session.GetProgress().Words["strasse"]);
            words.Words[0].Text = "STRASSE"; session.Load(words, session.GetProgress()); Equal(1, session.GetCount(ReviewFilter.Mastered));
        });
        Check("empty filter and restart from first word", () => {
            var session = new ReviewSession(); session.Load(ThreeWords(), new ReviewProgress()); Equal(false, session.Start(ReviewFilter.Mastered)); Equal(false, session.Move(-1));
            session.Start(ReviewFilter.All); Equal("a", session.CurrentWord!.Text); Equal(false, session.Move(-1));
        });
    }
}
