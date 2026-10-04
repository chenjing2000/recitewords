using System.ComponentModel;
using ReciteWords.Models;
namespace ReciteWords.ViewModels;

public class FilterChoice : INotifyPropertyChanged
{
    private readonly string name;
    public event PropertyChangedEventHandler? PropertyChanged;
    public ReviewFilter Value { get; }
    public string Label { get; private set; }
    public FilterChoice(ReviewFilter value, string label) { Value = value; name = label; Label = label + "(0)"; }
    public void UpdateCount(int count)
    {
        string text = name + "(" + count + ")";
        if (text == Label) return;
        Label = text; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
    }
}
