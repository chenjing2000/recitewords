using ReciteWords.Common;
using ReciteWords.Models;
namespace ReciteWords.Review;

public class ReviewSession : IReviewSession
{
    private WordList wordList = new WordList();
    private ReviewSnapshot state = new ReviewSnapshot();
    public Word? CurrentWord => Completed || Count == 0 ? null : state.Queue[state.Position];
    public ReviewFilter Filter => state.Filter;
    public int Count => state.Queue.Count;
    public bool Completed => state.Completed;
    public bool CanMovePrevious => Count > 0 && (Completed || state.Position > 0);
    public bool CanMoveNext => !Completed && state.Position + 1 < Count;
    public void Load(WordList words, ReviewProgress progress)
    {
        wordList = words;
        state = new ReviewSnapshot { Progress = CopyProgress(progress) };
        Start(ReviewFilter.All);
    }
    private bool Matches(Word word, ReviewFilter filter)
    {
        if (filter == ReviewFilter.All) return true;
        bool rated = state.Progress.Words.TryGetValue(Spelling.Fold(word.Text), out var level);
        if (filter == ReviewFilter.Unseen) return !rated;
        if (!rated) return false;
        if (filter == ReviewFilter.Unknown) return level == StudyLevel.Unknown;
        if (filter == ReviewFilter.Familiar) return level == StudyLevel.Familiar;
        return filter == ReviewFilter.Mastered && level == StudyLevel.Mastered;
    }
    public int GetCount(ReviewFilter filter)
    {
        int count = 0;
        foreach (var word in wordList.Words) if (Matches(word, filter)) count++;
        return count;
    }
    public bool Start(ReviewFilter filter)
    {
        state.Filter = filter; state.Queue = new List<Word>();
        foreach (var word in wordList.Words) if (Matches(word, filter)) state.Queue.Add(word);
        state.Position = 0;
        state.Completed = Count == 0;
        return Count > 0;
    }
    public bool Move(int offset)
    {
        if (offset == -1 && CanMovePrevious)
        {
            if (Completed) state.Completed = false;
            else state.Position--;
            return true;
        }
        if (offset == 1 && CanMoveNext) { state.Position++; return true; }
        return false;
    }
    public void Rate(StudyLevel level)
    {
        var word = CurrentWord; if (word == null) return;
        state.Progress.Words[Spelling.Fold(word.Text)] = level;
        if (state.Position == Count - 1) state.Completed = true;
        else state.Position++;
    }
    public ReviewProgress GetProgress() => CopyProgress(state.Progress);
    public ReviewSnapshot Capture() => CopyState(state);
    public void Restore(ReviewSnapshot snapshot) { state = CopyState(snapshot); }
    private static ReviewProgress CopyProgress(ReviewProgress progress)
    {
        var copy = new ReviewProgress { Application = progress.Application, SchemaVersion = progress.SchemaVersion };
        foreach (var item in progress.Words) copy.Words.Add(item.Key, item.Value);
        return copy;
    }
    private static ReviewSnapshot CopyState(ReviewSnapshot snapshot)
    {
        return new ReviewSnapshot
        {
            Progress = CopyProgress(snapshot.Progress),
            Filter = snapshot.Filter,
            Queue = new List<Word>(snapshot.Queue),
            Position = snapshot.Position,
            Completed = snapshot.Completed
        };
    }
}
