using ReciteWords.Models;
namespace ReciteWords.Review;

public interface IReviewSession
{
    void Load(WordList words, ReviewProgress progress);
    int GetCount(ReviewFilter filter);
    bool Start(ReviewFilter filter, int number);
    bool Move(int offset);
    void Rate(StudyLevel level);
    ReviewProgress Capture();
    void Restore(ReviewProgress progress);
    Word? CurrentWord { get; }
    ReviewFilter Filter { get; }
    int Count { get; }
    bool Completed { get; }
    bool CanMovePrevious { get; }
    bool CanMoveNext { get; }
}
