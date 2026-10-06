using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Diagnostics;
using ReciteWords.Audio;
using ReciteWords.Storage;
using ReciteWords.Review;
using ReciteWords.ViewModels;
using ReciteWords.Views;
namespace ReciteWords.Tests;
internal static partial class Program
{
    static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
    static void UiTests()
    {
        Check("WPF sample render, actual bounds, rate and restore", () => {
            var app = new ReciteWords.App(); app.InitializeComponent(); app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            string folder = Path.Combine(Temp, "sample-qa"); Directory.CreateDirectory(folder);
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.json"), Path.Combine(folder, "sample.json"));
            string settingsPath = Path.Combine(folder, "settings.tmp");
            var testPlayer = new TestPlayer();
            var vm = new MainViewModel(new WordListRepository(), new ProgressRepository(), new SettingsRepository(settingsPath), new ReviewSession(), new AudioCatalog(), testPlayer, action => Dispatcher.CurrentDispatcher.BeginInvoke(action, DispatcherPriority.Background));
            var window = new MainWindow(vm); window.Show(); vm.SelectFolder(folder); vm.RevealDefinition(); Pump(); window.UpdateLayout();
            Equal(true, Math.Abs(window.FontSize - 11.0 * 96 / 72) < 0.001);
            Equal(0, VisualChildren(window).OfType<Slider>().Count());
            var bookCombo = (ComboBox)window.FindName("WordListCombo");
            Equal("sample", bookCombo.Text);
            var filterCombo = (ComboBox)window.FindName("ReviewFilterCombo");
            Equal("全部(6)", vm.Filters[0].Label); Equal("未学(6)", vm.Filters[1].Label);
            Equal("全部(6)", ((FilterChoice)filterCombo.SelectedItem).Label);
            var familiarButton = VisualChildren(window).OfType<Button>().First(button => Equals(button.Content, "认识"));
            Equal(true, window.Icon != null);
            var audioButtons = VisualChildren(window).OfType<Button>().Where(button => button.ToolTip is string tip && (tip == "单词英音" || tip == "单词美音" || tip == "例句英音" || tip == "例句美音")).ToList();
            Equal(4, audioButtons.Count);
            foreach (var button in audioButtons)
            {
                Equal("🔊", button.Content);
                Equal("Segoe UI", button.FontFamily.Source);
                Equal(0, VisualChildren(button).OfType<Image>().Count());
                Equal(new Thickness(0), button.BorderThickness);
                Equal("#00FFFFFF", button.Background.ToString());
                Equal(false, button.IsEnabled);
            }
            foreach (var button in VisualChildren(window).OfType<Button>())
            {
                if (audioButtons.Contains(button)) continue;
                Equal(familiarButton.Background.ToString(), button.Background.ToString());
                Equal(familiarButton.BorderBrush.ToString(), button.BorderBrush.ToString());
                Equal(familiarButton.Foreground.ToString(), button.Foreground.ToString());
            }
            var editor = (RichTextBox)((DefinitionView)window.FindName("Definition")).FindName("ContentEditor");
            var paragraphs = editor.Document.Blocks.OfType<Paragraph>().ToList();
            var definitionRuns = paragraphs.SelectMany(paragraph => paragraph.Inlines.OfType<Run>()).ToList();
            foreach (var text in new[] { "adj.  ", "aware of and understanding a fact or situation", "意识到的；了解的" })
                Equal("#FFC8161D", definitionRuns.First(run => run.Text == text).Foreground.ToString());
            var exampleTitle = definitionRuns.First(run => run.Text == "Example:  ");
            var exampleBlock = paragraphs.First(paragraph => paragraph.Inlines.Contains(exampleTitle));
            double expectedIndent = 2 * window.FontSize;
            Equal(expectedIndent, exampleBlock.Margin.Left); Equal(-expectedIndent, exampleBlock.TextIndent);
            var translation = exampleBlock.Inlines.OfType<Run>().Last();
            double fieldLeft = exampleTitle.ContentStart.GetCharacterRect(LogicalDirection.Forward).Left;
            double translationLeft = translation.ContentStart.GetCharacterRect(LogicalDirection.Forward).Left;
            Equal(true, Math.Abs(translationLeft - fieldLeft - expectedIndent) < 1);
            var ending = (StackPanel)exampleBlock.Inlines.OfType<InlineUIContainer>().Single().Child;
            Equal("例句英音", ((Button)ending.Children[1]).ToolTip);
            Equal("例句美音", ((Button)ending.Children[2]).ToolTip);
            Equal(true, ((TextBlock)ending.Children[0]).Text.Length > 0);
            Equal("#FF007175", exampleTitle.Foreground.ToString());
            foreach (var title in new[] { "Synonyms", "Antonyms", "Collocations", "Notes", "Etymology" })
                Equal("#FF007175", definitionRuns.First(run => run.Text == title + ":  ").Foreground.ToString());
            Equal(true, paragraphs.IndexOf(paragraphs.First(p => p.Inlines.OfType<Run>().Any(r => r.Text == "Etymology:  "))) < paragraphs.IndexOf(paragraphs.First(p => p.Inlines.OfType<Run>().Any(r => r.Text == "Notes:  "))));
            Equal(5 * window.FontSize, ((Border)window.FindName("DefinitionBorder")).MinHeight);
            var indent = (Thickness)window.Resources["FilterTextIndent"];
            var space = new FormattedText(" ", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(window.FontFamily, window.FontStyle, window.FontWeight, window.FontStretch), window.FontSize, Brushes.Black, VisualTreeHelper.GetDpi(window).PixelsPerDip);
            Equal(space.WidthIncludingTrailingWhitespace, indent.Left);
            Equal(window.Foreground.ToString(), definitionRuns.First(run => run.Text.StartsWith("Cognizant that")).Foreground.ToString());
            double originalFilterWidth = filterCombo.ActualWidth;
            double originalMinimumWidth = window.MinWidth;
            vm.Filters[0].UpdateCount(123456); vm.RevealDefinition(); Pump(); window.UpdateLayout();
            Equal(true, filterCombo.ActualWidth > originalFilterWidth);
            Equal(true, window.MinWidth > originalMinimumWidth);
            Equal("全部(123456)", ((FilterChoice)filterCombo.SelectedItem).Label);
            vm.Filters[0].UpdateCount(6); vm.RevealDefinition(); Pump(); window.UpdateLayout();
            exampleTitle = editor.Document.Blocks.OfType<Paragraph>().SelectMany(p => p.Inlines.OfType<Run>()).First(r => r.Text == "Example:  ");
            Equal(null, window.FindName("ReviewButton"));
            Equal(null, window.FindName("PositionTextLabel"));
            Equal(0, VisualChildren(window).OfType<TextBox>().Count());
            var previous = (Button)window.FindName("PreviousButton");
            var next = (Button)window.FindName("NextButton");
            var unknown = (Button)window.FindName("UnknownButton");
            var explanation = (Button)window.FindName("ExplanationButton");
            vm.RevealCommand.Execute(null); Pump(); Equal(false, vm.DefinitionVisible);
            Equal(0, editor.Document.Blocks.Count);
            vm.RevealCommand.Execute(null); Pump(); Equal(true, vm.DefinitionVisible);
            Equal(true, editor.Document.Blocks.Count > 0);
            exampleTitle = editor.Document.Blocks.OfType<Paragraph>().SelectMany(p => p.Inlines.OfType<Run>()).First(r => r.Text == "Example:  ");
            foreach (var pair in new[] { ("FolderButton", "OpenFolderIcon"), ("PreviousButton", "PreviousWordIcon"), ("NextButton", "NextWordIcon"), ("ExplanationButton", "ExplanationIcon") })
                Equal(window.FindResource(pair.Item2), VisualChildren((Button)window.FindName(pair.Item1)).OfType<Image>().Single().Source);
            double fixedFilterWidth = filterCombo.ActualWidth;
            double previousWidth = previous.ActualWidth;
            double nextWidth = next.ActualWidth;
            var wordLabel = VisualChildren(window).OfType<TextBlock>().First(text => text.Text == "cognizant");
            Equal(16.0, wordLabel.FontSize);
            Equal("#FFB81A35", ((SolidColorBrush)wordLabel.Foreground).Color.ToString());
            var root = (Grid)window.Content;
            Equal(true, Math.Abs(wordLabel.TranslatePoint(new Point(), root).X + wordLabel.ActualWidth / 2 - root.ActualWidth / 2) < 1);
            var phoneticRow = (StackPanel)VisualTreeHelper.GetParent(VisualChildren(window).OfType<TextBlock>().First(text => text.Text == vm.phonetic_uk));
            var ukLabel = (TextBlock)window.FindName("UkPhoneticLabel");
            var usLabel = (TextBlock)window.FindName("UsPhoneticLabel");
            var wordUk = (Button)window.FindName("WordUkButton");
            var wordUs = (Button)window.FindName("WordUsButton");
            Equal(vm.phonetic_uk, ukLabel.Text); Equal(vm.phonetic_us, usLabel.Text);
            Equal(true, ukLabel.TranslatePoint(new Point(), phoneticRow).X < wordUk.TranslatePoint(new Point(), phoneticRow).X);
            Equal(true, wordUk.TranslatePoint(new Point(), phoneticRow).X < usLabel.TranslatePoint(new Point(), phoneticRow).X);
            Equal(true, usLabel.TranslatePoint(new Point(), phoneticRow).X < wordUs.TranslatePoint(new Point(), phoneticRow).X);
            Equal(true, Math.Abs(phoneticRow.TranslatePoint(new Point(), root).X + phoneticRow.ActualWidth / 2 - root.ActualWidth / 2) < 1);
            Equal(79.0, filterCombo.ActualWidth);
            Equal(32.0, filterCombo.ActualHeight);
            double wideBookWidth = bookCombo.ActualWidth;
            window.Width = window.MinWidth; window.Height = window.MinHeight; Pump(); window.UpdateLayout();

            Equal(true, bookCombo.ActualWidth < wideBookWidth);
            Equal(fixedFilterWidth, filterCombo.ActualWidth);
            Equal(previousWidth, previous.ActualWidth); Equal(nextWidth, next.ActualWidth);
            Equal(true, Math.Abs(unknown.TranslatePoint(new Point(), window).Y - filterCombo.TranslatePoint(new Point(), window).Y) < 1);
            Equal(true, next.TranslatePoint(new Point(), window).X + next.ActualWidth < unknown.TranslatePoint(new Point(), window).X);
            Equal(true, next.TranslatePoint(new Point(), window).X < explanation.TranslatePoint(new Point(), window).X);
            Equal(true, explanation.TranslatePoint(new Point(), window).X < unknown.TranslatePoint(new Point(), window).X);
            var message = (TextBlock)window.FindName("InformationText");
            var informationBar = (Grid)window.FindName("InformationBar");
            Equal(Visibility.Collapsed, informationBar.Visibility);
            foreach (var text in VisualChildren(window).OfType<TextBlock>())
                if (text != wordLabel) Equal(true, Math.Abs(text.FontSize - window.FontSize) < 0.001);
            var definitionAtMinimum = (DefinitionView)window.FindName("Definition");
            var viewport = (ScrollViewer)definitionAtMinimum.FindName("Scroller");
            Equal(true, viewport.ViewportHeight >= 5 * window.FontSize - 34 - 1);
            var verticalBar = (System.Windows.Controls.Primitives.ScrollBar)viewport.Template.FindName("PART_VerticalScrollBar", viewport);
            Equal(7.0, verticalBar.ActualWidth);
            foreach (var control in VisualChildren(window).OfType<Control>().Where(control => control is Button || control is ComboBox || control is TextBox))
            {
                bool isAudio = control.ToolTip is string tip && (tip == "单词英音" || tip == "单词美音" || tip == "例句英音" || tip == "例句美音");
                Equal(isAudio ? 20.0 : 32.0, control.ActualHeight);
                var origin = control.TranslatePoint(new Point(), root);
                Equal(true, origin.X >= -0.5 && origin.X + control.ActualWidth <= root.ActualWidth + 0.5);
            }
            Equal("cognizant", vm.CurrentWord!.Text);
            Equal(true, window.ActualWidth <= window.MaxWidth + 1); Equal(true, window.ActualHeight <= window.MaxHeight + 1);
            double originalLeft = window.Left;
            double movingLeft = originalLeft + window.MaxWidth + 30;
            window.Left = movingLeft; Pump(); Equal(movingLeft, window.Left);
            window.Left = originalLeft; Pump();
            string output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../artifacts/qa")); Directory.CreateDirectory(output);
            Capture(window, Path.Combine(output, "minimum-width.png"));
            window.Height = Math.Min(window.MaxHeight, window.MinHeight + 260); Pump(); window.UpdateLayout();
            var continuation = exampleTitle.ContentStart.GetLineStartPosition(1)!;
            double continuationLeft = continuation.GetCharacterRect(LogicalDirection.Forward).Left;
            double narrowFieldLeft = exampleTitle.ContentStart.GetCharacterRect(LogicalDirection.Forward).Left;
            Equal(true, Math.Abs(continuationLeft - narrowFieldLeft - expectedIndent) < 1);
            Capture(window, Path.Combine(output, "inline-example-narrow.png"));
            window.Width = window.MaxWidth; window.Height = window.MaxHeight * 0.9; Pump(); window.UpdateLayout();
            Capture(window, Path.Combine(output, "sample-definition.png"));
            window.Height = window.MinHeight; Pump(); window.UpdateLayout();
            var definition = (DefinitionView)window.FindName("Definition");
            var scroller = (ScrollViewer)definition.FindName("Scroller");
            scroller.ScrollToBottom(); Pump(); Equal(true, scroller.VerticalOffset > 0);
            double beforeWheel = scroller.VerticalOffset;
            editor.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, 120) { RoutedEvent = System.Windows.Input.Mouse.PreviewMouseWheelEvent });
            Pump(); Equal(true, scroller.VerticalOffset < beforeWheel);
            scroller.ScrollToBottom(); Pump();
            Capture(window, Path.Combine(output, "sample-etymology.png"));
            vm.Navigate(1); Pump(); Equal(false, vm.DefinitionVisible); Equal(0.0, scroller.VerticalOffset); vm.Navigate(-1); Pump();
            window.WindowState = WindowState.Maximized; Pump();
            Equal(true, window.ActualWidth <= window.MaxWidth + 1); Equal(true, window.ActualHeight <= window.MaxHeight + 1);
            window.WindowState = WindowState.Normal; Pump();
            vm.Rate(ReciteWords.Models.StudyLevel.Mastered); Equal("address", vm.CurrentWord!.Text); vm.Navigate(-1); vm.Rate(ReciteWords.Models.StudyLevel.Unknown);
            Pump(); Equal("未学(5)", vm.Filters[1].Label); Equal("不懂(1)", vm.Filters[2].Label); Equal("掌握(0)", vm.Filters[4].Label);
            filterCombo.IsDropDownOpen = true; Pump();
            Capture(window, Path.Combine(output, "filter-counts.png"));
            filterCombo.IsDropDownOpen = false; Pump();
            window.Height = Math.Min(window.MaxHeight, window.MinHeight + 100); Pump(); window.UpdateLayout();
            var definitionBorder = (Border)window.FindName("DefinitionBorder");
            double restingDefinitionHeight = definitionBorder.ActualHeight;
            double restingButtonsY = unknown.TranslatePoint(new Point(), window).Y;
            vm.Warn("第一行警告\n第二行说明"); Pump(); window.UpdateLayout(); Equal("第一行警告 · 第二行说明", message.Text);
            Equal(Visibility.Visible, informationBar.Visibility);
            Equal(true, definitionBorder.ActualHeight < restingDefinitionHeight);
            Equal(true, unknown.TranslatePoint(new Point(), window).Y < restingButtonsY);
            Capture(window, Path.Combine(output, "information-bar.png"));
            var warningElapsed = Stopwatch.StartNew();
            while (warningElapsed.Elapsed < TimeSpan.FromSeconds(4.5)) { Pump(); Thread.Sleep(20); }
            Equal(true, vm.HasWarning);
            while (warningElapsed.Elapsed < TimeSpan.FromSeconds(5.4)) { Pump(); Thread.Sleep(20); }
            Equal(false, vm.HasWarning); Equal("", message.Text); window.UpdateLayout();
            Equal(Visibility.Collapsed, informationBar.Visibility);
            Equal(true, Math.Abs(definitionBorder.ActualHeight - restingDefinitionHeight) < 1);
            Equal(true, Math.Abs(unknown.TranslatePoint(new Point(), window).Y - restingButtonsY) < 1);
            Capture(window, Path.Combine(output, "information-collapsed.png"));
            string longFolder = Path.Combine(Temp, "long-phonetics"); Directory.CreateDirectory(longFolder);
            string longPath = Path.Combine(longFolder, "long-phonetic.json");
            string longPhonetic = "/" + new string('a', 300) + "/";
            File.WriteAllText(longPath, Minimal.Replace("/əˈweə/", longPhonetic).Replace("/əˈwer/", longPhonetic));
            vm.SelectFolder(longFolder); Pump(); window.UpdateLayout();
            Equal(true, window.MinWidth <= window.MaxWidth);
            Equal(true, window.ActualWidth <= window.MaxWidth + 1);
            Equal(longPhonetic, ukLabel.ToolTip); Equal(longPhonetic, usLabel.ToolTip);
            foreach (var button in new[] { wordUk, wordUs })
                Equal(true, button.TranslatePoint(new Point(), root).X + button.ActualWidth <= root.ActualWidth + 1);
            window.Left += 1; Pump();
            Capture(window, Path.Combine(output, "long-phonetic.png"));
            Check("example audio focus and playback preserve scrolled definition", () => VerifyExampleScroll(window, vm, testPlayer));
            vm.SelectFolder(folder); Pump();
            window.Close();
            var restored = new ReviewSession(); restored.Load(new WordListRepository().Load(Path.Combine(folder, "sample.json")), new ProgressRepository().LoadOrCreate(Path.Combine(folder, "sample.json")));
            Equal("cognizant", restored.CurrentWord!.Text); Equal(ReciteWords.Models.StudyLevel.Unknown, restored.GetProgress().Words["cognizant"]);
            File.WriteAllText(Path.Combine(output, "ui-report.txt"), $"Window DIP: {window.ActualWidth} x {window.ActualHeight}; max {window.MaxWidth} x {window.MaxHeight}\nSample: 6 words; rating and reopen passed.\n");
            app.Shutdown();
        });
        Check("WPF MediaPlayer opens and completes local WAV", () => {
            string path = Path.Combine(Temp, "test-tone.wav");
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                const int rate = 22050; const int frames = rate / 4;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + frames * 2); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16); writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(frames * 2);
                for (int i = 0; i < frames; i++) writer.Write((short)(Math.Sin(i * 2 * Math.PI * 440 / rate) * 800));
            }
            VerifyMedia(path);
        });
    }
    static void VerifyExampleScroll(MainWindow window, MainViewModel vm, TestPlayer player)
    {
        string folder = Path.Combine(Temp, "scrolled-examples"); Directory.CreateDirectory(folder);
        var senses = Enumerable.Range(1, 8).Select(index => new {
            pos = "n.", chinese_meaning = "用于验证滚动位置的词义。",
            eid = index.ToString("D2"), example = "This example sentence has a local audio file.",
            example_translation = "这条例句用于检查播放时内容区保持在用户选择的位置。"
        }).ToArray();
        File.WriteAllText(Path.Combine(folder, "scroll.json"), System.Text.Json.JsonSerializer.Serialize(new {
            words = new[] { new { word = "scroll", phonetic_uk = "", phonetic_us = "", senses } }
        }));
        string audioFolder = Path.Combine(folder, "scroll", "examples"); Directory.CreateDirectory(audioFolder);
        foreach (string accent in new[] { "uk", "us" }) File.WriteAllBytes(Path.Combine(audioFolder, "scroll_e08_" + accent + ".wav"), Array.Empty<byte>());
        vm.SelectFolder(folder); vm.RevealDefinition(); Pump();
        window.Height = window.MinHeight + 100; Pump(); window.UpdateLayout();
        var definition = (DefinitionView)window.FindName("Definition");
        var scroller = (ScrollViewer)definition.FindName("Scroller");
        var editor = (RichTextBox)definition.FindName("ContentEditor");
        var document = editor.Document;
        foreach (string accent in new[] { "uk", "us" })
        {
            var button = VisualChildren(definition).OfType<Button>().Last(b => Equals(b.ToolTip, accent == "uk" ? "例句英音" : "例句美音"));
            Equal(true, button.IsEnabled);
            scroller.ScrollToBottom(); Pump(); double offset = scroller.VerticalOffset; Equal(true, offset > 0);
            Equal(true, button.Focus()); Pump();
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
            Equal(Path.Combine(audioFolder, "scroll_e08_" + accent + ".wav"), player.Played.Last());
            Equal(true, ReferenceEquals(document, editor.Document));
            if (Math.Abs(offset - scroller.VerticalOffset) > 1)
                throw new Exception($"Example {accent} playback moved scroll from {offset} to {scroller.VerticalOffset}");
        }
    }
    static IEnumerable<DependencyObject> VisualChildren(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var descendant in VisualChildren(child)) yield return descendant;
        }
    }
    static void VerifyMedia(string path)
    {
        var media = new MediaPlayer(); bool opened = false; bool ended = false; string? error = null;
        media.MediaOpened += (_, _) => opened = true; media.MediaEnded += (_, _) => ended = true; media.MediaFailed += (_, e) => error = e.ErrorException.Message;
        media.Open(new Uri(Path.GetFullPath(path))); media.Volume = 0; media.Play();
        var elapsed = Stopwatch.StartNew();
        while (!ended && error == null && elapsed.Elapsed < TimeSpan.FromSeconds(8)) { Pump(); Thread.Sleep(15); }
        media.Close();
        if (error != null) throw new Exception(error);
        Equal(true, opened); Equal(true, ended);
    }
    static void Capture(Window window, string path)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = File.Create(path); encoder.Save(output);
    }
}

