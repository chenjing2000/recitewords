using System.Windows;
using System.Windows.Threading;
using ReciteWords.Storage;
using ReciteWords.Review;
using ReciteWords.Audio;
using ReciteWords.ViewModels;
using ReciteWords.Views;
namespace ReciteWords;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var vm = new MainViewModel(new WordListRepository(), new ProgressRepository(),
            new SettingsRepository(Path.Combine(AppContext.BaseDirectory, "recitewords.settings.json")),
            new ReviewSession(), new AudioCatalog(), new WpfAudioPlayer(),
            action => Dispatcher.BeginInvoke(action, DispatcherPriority.Background));
        var window = new MainWindow(vm); MainWindow = window;
        window.Show(); vm.Initialize(Path.Combine(AppContext.BaseDirectory, "wordlist"));
    }
}
