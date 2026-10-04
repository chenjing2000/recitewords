using ReciteWords.Models;
namespace ReciteWords.Storage;

public interface IProgressRepository
{
    ReviewProgress LoadOrCreate(string wordListPath, WordList wordList);
    void Save(string wordListPath, ReviewProgress progress);
}
