using ReciteWords.Models;
using ReciteWords.Storage;
using ReciteWords.Review;
namespace ReciteWords.Tests;
internal static partial class Program
{
    static WordList ThreeWords()
    {
        var result = new WordList { Name = "Sample" };
        foreach (string id in new[] { "a", "b", "c" }) result.Words.Add(new Word { Wid = id, Text = id, Senses = new List<WordSense>() { new WordSense { ChineseMeaning = id } } });
        return result;
    }
    static void ProgressTests()
    {
        var repository = new ProgressRepository();
        Check("userdata initialization and Python untouched", () => {
            string path = Write("education.json", Minimal);
            var python = Path.Combine(Temp, "education"); Directory.CreateDirectory(python);
            File.WriteAllText(Path.Combine(python, "progress.json"), "python untouched");
            var progress = repository.LoadOrCreate(path, ThreeWords());
            Equal(true, File.Exists(Path.Combine(Temp, "userdata", "education.progress.json")));
            Equal(3, progress.Session.Queue.Count); Equal(false, progress.Session.Completed);
            Equal("python untouched", File.ReadAllText(Path.Combine(python, "progress.json")));
        });
        Check("save and reopen restores exact progress", () => {
            string path = Write("saved.json", Minimal);
            var progress = repository.LoadOrCreate(path, ThreeWords());
            progress.Words["a"] = new WordProgress { Level = StudyLevel.Mastered, ReviewCount = 2 };
            progress.Session.Position = 1; repository.Save(path, progress);
            var reopened = repository.LoadOrCreate(path, ThreeWords());
            Equal(StudyLevel.Mastered, reopened.Words["a"].Level); Equal(2, reopened.Words["a"].ReviewCount); Equal(1, reopened.Session.Position);
            Equal(true, File.ReadAllText(ProgressRepository.ProgressPath(path)).Contains("review_count"));
        });
        Check("different wordlists isolated and rename creates", () => {
            var first = repository.LoadOrCreate(Write("first.json", Minimal), ThreeWords());
            first.Words["a"] = new WordProgress { Level = StudyLevel.Unknown, ReviewCount = 1 }; repository.Save(Path.Combine(Temp, "first.json"), first);
            Equal(0, repository.LoadOrCreate(Write("renamed.json", Minimal), ThreeWords()).Words.Count);
            Equal(1, repository.LoadOrCreate(Path.Combine(Temp, "first.json"), ThreeWords()).Words.Count);
        });
        Check("corrupt progress never overwritten", () => {
            string path = Write("corrupt.json", Minimal); string file = ProgressRepository.ProgressPath(path);
            File.WriteAllText(file, "{broken"); Reject(() => repository.LoadOrCreate(path, ThreeWords())); Equal("{broken", File.ReadAllText(file));
        });
        Check("wrong progress version type rejected without crash", () => {
            string path = Write("bad-version.json", Minimal); string file = ProgressRepository.ProgressPath(path);
            string text = "{\"application\":\"ReciteWords\",\"schema_version\":\"1\",\"words\":{},\"session\":{\"filter\":\"All\",\"queue\":[\"a\"],\"position\":0,\"completed\":false}}";
            File.WriteAllText(file, text); Reject(() => repository.LoadOrCreate(path, ThreeWords())); Equal(text, File.ReadAllText(file));
        });
        Check("failed atomic save leaves original bytes", () => {
            string path = Write("locked.json", Minimal); var progress = repository.LoadOrCreate(path, ThreeWords()); string file = ProgressRepository.ProgressPath(path);
            string before = File.ReadAllText(file);
            using (var handle = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bool failedSave = false; try { progress.Session.Position = 2; repository.Save(path, progress); } catch (IOException) { failedSave = true; }
                Equal(true, failedSave);
            }
            Equal(before, File.ReadAllText(file));
        });
        Check("inconsistent completed position rejected", () => {
            string path = Write("bad-completed.json", Minimal); string file = ProgressRepository.ProgressPath(path);
            string text = "{\"application\":\"ReciteWords\",\"schema_version\":1,\"words\":{},\"session\":{\"filter\":\"All\",\"queue\":[\"a\",\"b\",\"c\"],\"position\":0,\"completed\":true}}";
            File.WriteAllText(file, text); Reject(() => repository.LoadOrCreate(path, ThreeWords())); Equal(text, File.ReadAllText(file));
        });
    }
    static void ReviewTests()
    {
        Check("rating, navigation and rerating", () => {
            var session = new ReviewSession(); session.Load(ThreeWords(), new ReviewProgress()); session.Start(ReviewFilter.All, 1);
            session.Rate(StudyLevel.Mastered); Equal("b", session.CurrentWord!.Wid);
            session.Move(-1); Equal(1, session.Capture().Words["a"].ReviewCount);
            session.Rate(StudyLevel.Unknown); Equal(StudyLevel.Unknown, session.Capture().Words["a"].Level); Equal(2, session.Capture().Words["a"].ReviewCount); Equal("b", session.CurrentWord!.Wid);
        });
        Check("filtered snapshot survives status changes", () => {
            var session = new ReviewSession(); session.Load(ThreeWords(), new ReviewProgress()); session.Start(ReviewFilter.Unseen, 1);
            session.Rate(StudyLevel.Familiar); Equal(3, session.Count); session.Move(-1); Equal("a", session.CurrentWord!.Wid); Equal(2, session.GetCount(ReviewFilter.Unseen));
        });
        Check("completion back to last and repeat", () => {
            var session = new ReviewSession(); session.Load(ThreeWords(), new ReviewProgress()); session.Start(ReviewFilter.All, 99);
            Equal("c", session.CurrentWord!.Wid); session.Rate(StudyLevel.Mastered); Equal(true, session.Completed);
            Equal(true, session.Move(-1)); Equal("c", session.CurrentWord!.Wid); session.Rate(StudyLevel.Unknown); Equal(true, session.Completed);
        });
        Check("snapshot deep copy and reordered restore", () => {
            var session = new ReviewSession(); var words = ThreeWords(); session.Load(words, new ReviewProgress()); session.Start(ReviewFilter.All, 1);
            var snapshot = session.Capture(); session.Rate(StudyLevel.Familiar); Equal(0, snapshot.Words.Count);
            var saved = session.Capture(); words.Words.Reverse(); session.Load(words, saved); Equal("b", session.CurrentWord!.Wid);
        });
        Check("deleted current restores next surviving word", () => {
            var session = new ReviewSession(); var words = ThreeWords(); session.Load(words, new ReviewProgress()); session.Start(ReviewFilter.All, 2);
            var saved = session.Capture(); words.Words.RemoveAt(1); session.Load(words, saved); Equal("c", session.CurrentWord!.Wid);
        });
        Check("empty filter and clamped start", () => {
            var session = new ReviewSession(); session.Load(ThreeWords(), new ReviewProgress()); Equal(false, session.Start(ReviewFilter.Mastered, 1)); Equal(false, session.Move(-1));
            session.Start(ReviewFilter.All, 0); Equal("a", session.CurrentWord!.Wid); Equal(false, session.Move(-1));
        });
    }
}
