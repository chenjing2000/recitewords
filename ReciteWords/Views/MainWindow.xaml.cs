using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Threading;
using Microsoft.Win32;
using ReciteWords.ViewModels;
using ReciteWords.Platform;
namespace ReciteWords.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel vm;
    private readonly DispatcherTimer warningTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(5) };
    public MainWindow(MainViewModel vm)
    {
        this.vm = vm; InitializeComponent(); DataContext = vm;
        DefinitionBorder.MinHeight = 5 * FontSize;
        vm.FolderRequested += ChooseFolder; vm.WarningChanged += OnWarningChanged;
        vm.PropertyChanged += OnViewModelChanged;
        warningTimer.Tick += (_, _) => { warningTimer.Stop(); vm.ClearWarning(); };
        if (vm.HasWarning) OnWarningChanged(this, EventArgs.Empty);
        SourceInitialized += (_, _) => { WindowBounds.Attach(this); WindowBounds.ApplySavedBounds(this, vm.Settings); };
        Loaded += (_, _) => { UpdateFilterWidth(); UpdateMinimumSize(); };
        LayoutRoot.LayoutUpdated += (_, _) => UpdateMinimumSize();
        Closing += OnClosing;
        Closed += (_, _) => { warningTimer.Stop(); vm.PropertyChanged -= OnViewModelChanged; vm.FolderRequested -= ChooseFolder; vm.WarningChanged -= OnWarningChanged; vm.Dispose(); };
    }
    private void UpdateFilterWidth()
    {
        var space = new FormattedText(" ", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Brushes.Black,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        var indent = new Thickness(space.WidthIncludingTrailingWhitespace, 0, 0, 0);
        if (!Equals(Resources["FilterTextIndent"], indent)) Resources["FilterTextIndent"] = indent;
        double textWidth = 0;
        foreach (var filter in vm.Filters)
        {
            var text = new FormattedText(filter.Label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            textWidth = Math.Max(textWidth, text.WidthIncludingTrailingWhitespace);
        }
        ReviewFilterCombo.Width = Math.Ceiling(Math.Max(79, textWidth + indent.Left + 24));
    }
    private void UpdateMinimumSize()
    {
        if (!IsLoaded || LayoutRoot.ActualWidth <= 0) return;
        double reviewWidth = 0;
        foreach (var column in ReviewControls.ColumnDefinitions)
        {
            if (column.Width.IsStar) reviewWidth += column.MinWidth;
            else if (column.Width.IsAuto) reviewWidth += ReviewFilterCombo.Width;
            else reviewWidth += column.Width.Value;
        }
        double chromeWidth = ActualWidth - LayoutRoot.ActualWidth;
        double chromeHeight = ActualHeight - LayoutRoot.ActualHeight;
        double phoneticWidth = Math.Max(0, MaxWidth - chromeWidth);
        // 为两个喇叭和标签间距保留 74 DIP，超长音标不能扩大窗口上限。
        double labelWidth = Math.Max(0, (phoneticWidth - 74) / 2);
        if (UkPhoneticLabel.MaxWidth != labelWidth) UkPhoneticLabel.MaxWidth = labelWidth;
        if (UsPhoneticLabel.MaxWidth != labelWidth) UsPhoneticLabel.MaxWidth = labelWidth;
        double fixedHeight = 0;
        for (int index = 0; index < LayoutRoot.RowDefinitions.Count; index++)
            if (index != 3) fixedHeight += LayoutRoot.RowDefinitions[index].ActualHeight;
        // 保持固定行高度，释义外框至少为正文的 5em。
        double rowWidth = Math.Min(PhoneticRow.DesiredSize.Width, phoneticWidth);
        double contentWidth = Math.Max(Math.Max(222, reviewWidth), rowWidth);
        double minimumWidth = Math.Min(MaxWidth, Math.Ceiling(contentWidth + chromeWidth));
        double minimumHeight = Math.Ceiling(fixedHeight + DefinitionBorder.MinHeight + chromeHeight);
        if (MinWidth != minimumWidth) MinWidth = minimumWidth;
        if (MinHeight != minimumHeight) MinHeight = minimumHeight;
    }
    private void ChooseFolder(object? sender, EventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "选择单词本所在文件夹", Multiselect = false };
        if (Directory.Exists(vm.Settings.Folder)) dialog.InitialDirectory = vm.Settings.Folder;
        if (dialog.ShowDialog(this) == true) vm.SelectFolder(dialog.FolderName);
    }
    private void OnWarningChanged(object? sender, EventArgs e) { warningTimer.Stop(); warningTimer.Start(); }
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!vm.DefinitionVisible) Definition.ResetScroll();
        UpdateFilterWidth(); UpdateMinimumSize();
    }
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        vm.Settings.Left = bounds.Left; vm.Settings.Top = bounds.Top; vm.Settings.Width = bounds.Width; vm.Settings.Height = bounds.Height; vm.SaveSettings();
    }
}
