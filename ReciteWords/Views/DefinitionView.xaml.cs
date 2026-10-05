using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Input;
using ReciteWords.Models;
using ReciteWords.ViewModels;
namespace ReciteWords.Views;

public partial class DefinitionView : UserControl
{
    public static readonly DependencyProperty WordProperty = DependencyProperty.Register(nameof(Word), typeof(Word), typeof(DefinitionView), new PropertyMetadata(null, OnWordChanged));
    public Word? Word { get => (Word?)GetValue(WordProperty); set => SetValue(WordProperty, value); }
    public DefinitionView()
    {
        InitializeComponent();
        ContentEditor.PreviewMouseWheel += (_, e) =>
        {
            e.Handled = true;
            Scroller.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = MouseWheelEvent });
        };
    }
    private static void OnWordChanged(DependencyObject control, DependencyPropertyChangedEventArgs args) { ((DefinitionView)control).Render(); }
    public void ResetScroll() { Scroller.ScrollToTop(); }
    private void Render()
    {
        var document = new FlowDocument { PagePadding = new Thickness(0) };
        document.SetBinding(TextElement.FontSizeProperty, new Binding(nameof(FontSize)) { Source = this });
        document.SetBinding(TextElement.FontFamilyProperty, new Binding(nameof(FontFamily)) { Source = this });
        document.SetBinding(TextElement.ForegroundProperty, new Binding(nameof(Foreground)) { Source = this });
        ContentEditor.Document = document; ResetScroll();
        if (Word == null) return;
        foreach (var sense in Word.Senses)
        {
            var heading = CreateParagraph(true, 10);
            heading.Foreground = new SolidColorBrush(Color.FromRgb(200, 22, 29));
            heading.Inlines.Add(new Run(sense.Pos + "  ") { FontWeight = FontWeights.SemiBold });
            if (sense.Register.Count > 0) heading.Inlines.Add(new Run("[" + string.Join(" · ", sense.Register) + "]  ") { Foreground = new SolidColorBrush(Color.FromRgb(87, 116, 136)) });
            if (sense.EnglishMeaning.Length > 0) { heading.Inlines.Add(new Run(sense.EnglishMeaning)); heading.Inlines.Add(new LineBreak()); }
            heading.Inlines.Add(new Run(sense.ChineseMeaning)); document.Blocks.Add(heading);
            if (sense.Example.Length > 0)
            {
                AddExample(sense);
            }
            else if (sense.ExampleTranslation.Length > 0) AddParagraph("例句译文", sense.ExampleTranslation);
            AddParagraph("Synonyms", string.Join(" · ", sense.Synonyms));
            AddParagraph("Antonyms", string.Join(" · ", sense.Antonyms));
            AddParagraph("Collocations", string.Join(" · ", sense.Collocations));
            document.Blocks.Add(new BlockUIContainer(new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(234, 239, 244)) }) { Margin = new Thickness(0, 4, 0, 16) });
        }
        AddParagraph("Etymology", Word.Etymology); AddParagraph("Notes", Word.Notes);
    }
    private Paragraph CreateParagraph(bool startsWithField, double bottomMargin)
    {
        double indent = 2 * (double)FindResource("UiFontSize");
        return new Paragraph
        {
            LineHeight = 20,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            Margin = new Thickness(indent, 0, 0, bottomMargin),
            TextIndent = startsWithField ? -indent : 0
        };
    }
    private void AddExample(WordSense sense)
    {
        var block = CreateParagraph(true, 14);
        block.Inlines.Add(new Run("Example:  ") { FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0, 113, 117)) });
        string sentence = sense.Example.TrimEnd();
        int lastSpace = sentence.LastIndexOf(' ');
        if (lastSpace >= 0) block.Inlines.Add(new Run(sentence.Substring(0, lastSpace + 1)));
        // Keep the final word and both speakers together so the speakers never occupy a separate line.
        var ending = new StackPanel { Orientation = Orientation.Horizontal };
        ending.Children.Add(new TextBlock { Text = sentence.Substring(lastSpace + 1), VerticalAlignment = VerticalAlignment.Center });
        ending.Children.Add(ExampleButton(sense.Eid, "uk"));
        ending.Children.Add(ExampleButton(sense.Eid, "us"));
        block.Inlines.Add(new InlineUIContainer(ending) { BaselineAlignment = BaselineAlignment.Center });
        if (sense.ExampleTranslation.Length > 0)
        {
            block.Inlines.Add(new LineBreak());
            block.Inlines.Add(new Run(sense.ExampleTranslation));
        }
        ContentEditor.Document.Blocks.Add(block);
    }
    private void AddParagraph(string title, string text)
    {
        if (text.Length == 0) return;
        var block = CreateParagraph(true, 14);
        block.Inlines.Add(new Run(title + ":  ") { FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0, 113, 117)) }); block.Inlines.Add(new Run(text)); ContentEditor.Document.Blocks.Add(block);
    }
    private Button ExampleButton(string eid, string accent)
    {
        var vm = DataContext as MainViewModel;
        var button = new Button { Content = "🔊", Style = (Style)FindResource("AudioButton"), IsEnabled = vm?.ExampleAudio(eid, accent) != null, ToolTip = accent == "uk" ? "例句英音" : "例句美音" };
        System.Windows.Automation.AutomationProperties.SetName(button, accent == "uk" ? "播放例句英音" : "播放例句美音");
        button.Click += (_, _) => vm?.PlayExample(eid, accent);
        return button;
    }
}
