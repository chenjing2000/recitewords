using ReciteWords.Models;
namespace ReciteWords.Review;

// 仅用于内存中的保存失败回滚，不写入进度文件。
public class ReviewSnapshot
{
    public ReviewProgress Progress { get; set; } = new ReviewProgress();
    public ReviewFilter Filter { get; set; } = ReviewFilter.All;
    public List<Word> Queue { get; set; } = new List<Word>();
    public int Position { get; set; }
    public bool Completed { get; set; }
}
