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
        double textWidth = 0;
        foreach (var filter in vm.Filters)
        {
            var text = new FormattedText(filter.Label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            textWidth = Math.Max(textWidth, text.WidthIncludingTrailingWhitespace);
        }
        ReviewFilterCombo.Width = Math.Ceiling(Math.Max(79, textWidth + 24) * 1.2);
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
        double fixedHeight = 0;
        for (int index = 0; index < LayoutRoot.RowDefinitions.Count; index++)
            if (index != 3) fixedHeight += LayoutRoot.RowDefinitions[index].ActualHeight;
        // Keep the fixed rows intact and leave four 20-DIP text lines inside the padded viewport.
        double minimumWidth = Math.Ceiling(Math.Max(222, reviewWidth) + chromeWidth);
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
