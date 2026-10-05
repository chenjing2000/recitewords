using ReciteWords.Models;
namespace ReciteWords.Storage;

public interface IProgressRepository
{
    ReviewProgress LoadOrCreate(string wordListPath);
    void Save(string wordListPath, ReviewProgress progress);
}
