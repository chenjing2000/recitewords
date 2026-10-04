using ReciteWords.Models;
namespace ReciteWords.Review;

public class ReviewSession : IReviewSession
{
    private WordList wordList = new WordList();
    private ReviewProgress progress = new ReviewProgress();
    private readonly Dictionary<string, Word> byId = new Dictionary<string, Word>(StringComparer.Ordinal);
    public Word? CurrentWord
    {
        get
        {
            if (Completed || Count == 0) return null;
            return byId[progress.Session.Queue[progress.Session.Position]];
        }
    }
    public ReviewFilter Filter { get { return progress.Session.Filter; } }
    public int Count => progress.Session.Queue.Count;
    public bool Completed => progress.Session.Completed;
    public bool CanMovePrevious => Count > 0 && (Completed || progress.Session.Position > 0);
    public bool CanMoveNext => !Completed && progress.Session.Position + 1 < Count;
    public void Load(WordList words, ReviewProgress data)
    {
        wordList = words; byId.Clear();
        foreach (var word in words.Words) byId.Add(word.Wid, word);
        progress = Copy(data);
        var old = progress.Session;
        if (old.Queue.Count == 0 && !old.Completed) { Start(ReviewFilter.All, 1); return; }
        string? current = null;
        if (!old.Completed)
            for (int i = old.Position; i < old.Queue.Count; i++)
                if (byId.ContainsKey(old.Queue[i])) { current = old.Queue[i]; break; }
        var retained = new List<string>();
        foreach (string id in old.Queue) if (byId.ContainsKey(id)) retained.Add(id);
        old.Queue = retained;
        if (old.Completed || current == null)
        { old.Completed = true; old.Position = Math.Max(0, retained.Count - 1); }
        else { old.Position = retained.IndexOf(current); }
    }
    private bool Matches(Word word, ReviewFilter filter)
    {
        if (filter == ReviewFilter.All) return true;
        bool rated = progress.Words.TryGetValue(word.Wid, out var item);
        if (filter == ReviewFilter.Unseen) return !rated;
        if (!rated) return false;
        if (filter == ReviewFilter.Unknown) return item!.Level == StudyLevel.Unknown;
        if (filter == ReviewFilter.Familiar) return item!.Level == StudyLevel.Familiar;
        return filter == ReviewFilter.Mastered && item!.Level == StudyLevel.Mastered;
    }
    private List<string> Select(ReviewFilter filter)
    {
        var ids = new List<string>();
        foreach (var word in wordList.Words)
            if (Matches(word, filter)) ids.Add(word.Wid);
        return ids;
    }
    public int GetCount(ReviewFilter filter)
    {
        int count = 0;
        foreach (var word in wordList.Words)
            if (Matches(word, filter)) count++;
        return count;
    }
    public bool Start(ReviewFilter filter, int number)
    {
        var ids = Select(filter);
        progress.Session = new ReviewSessionState { Filter = filter, Queue = ids, Position = ids.Count == 0 ? 0 : Math.Clamp(number, 1, ids.Count) - 1, Completed = ids.Count == 0 };
        return ids.Count > 0;
    }
    public bool Move(int offset)
    {
        if (offset == -1 && CanMovePrevious)
        {
            if (Completed) progress.Session.Completed = false;
            else progress.Session.Position--;
            return true;
        }
        if (offset == 1 && CanMoveNext) { progress.Session.Position++; return true; }
        return false;
    }
    public void Rate(StudyLevel level)
    {
        var word = CurrentWord; if (word == null) return;
        if (!progress.Words.TryGetValue(word.Wid, out var item)) { item = new WordProgress(); progress.Words.Add(word.Wid, item); }
        item.Level = level; item.ReviewCount++;
        if (progress.Session.Position == Count - 1) progress.Session.Completed = true;
        else progress.Session.Position++;
    }
    public ReviewProgress Capture() => Copy(progress);
    public void Restore(ReviewProgress data) { progress = Copy(data); }
    private static ReviewProgress Copy(ReviewProgress data)
    {
        var copy = new ReviewProgress
        {
            Application = data.Application,
            SchemaVersion = data.SchemaVersion,
            Session = new ReviewSessionState { Filter = data.Session.Filter, Queue = new List<string>(data.Session.Queue), Position = data.Session.Position, Completed = data.Session.Completed }
        };
        foreach (var item in data.Words) copy.Words.Add(item.Key, new WordProgress { Level = item.Value.Level, ReviewCount = item.Value.ReviewCount });
        return copy;
    }
}
