using ReciteWords.Models;
using ReciteWords.Audio;
using ReciteWords.Storage;
using ReciteWords.Review;
using ReciteWords.ViewModels;
namespace ReciteWords.Tests;
internal static partial class Program
{
    class TestPlayer : IAudioPlayer
    {
        public List<string> Played = new List<string>();
        public int Stops;
        public event EventHandler<string>? PlaybackFailed { add { } remove { } }
        public void Play(string path) { Played.Add(path); }
        public void Stop() { Stops++; }
        public void Dispose() { }
    }
    class TestCatalog : IAudioCatalog
    {
        public void Load(string path, WordList words) { }
        public string? FindWord(string word, string accent) => accent == "uk" ? word + "-uk.mp3" : word + "-us.mp3";
        public string? FindExample(string word, string eid, string accent) => null;
    }
    class TestProgress : IProgressRepository
    {
        public bool FailSave;
        public int Saves;
        public ReviewProgress LoadOrCreate(string path)
        {
            return new ReviewProgress();
        }
        public void Save(string path, ReviewProgress progress) { Saves++; if (FailSave) throw new IOException("test locked"); }
    }
    static void ViewModelTests()
    {
        string folder = Path.Combine(Temp, "vm"); Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "sample.json"), "{\"words\":[{\"word\":\"a\",\"phonetic_uk\":\"\",\"phonetic_us\":\"\",\"senses\":[{\"pos\":\"n.\",\"chinese_meaning\":\"甲\"}]},{\"word\":\"b\",\"phonetic_uk\":\"\",\"phonetic_us\":\"\",\"senses\":[{\"pos\":\"n.\",\"chinese_meaning\":\"乙\"}]}]}");
        Check("folder without valid wordlists is remembered after restart", () => {
            string emptyFolder = Path.Combine(Temp, "empty-folder"); Directory.CreateDirectory(emptyFolder);
            File.WriteAllText(Path.Combine(emptyFolder, "bad.json"), "{}");
            var settings = new SettingsRepository(Path.Combine(Temp, "remember-folder.tmp"));
            using (var vm = new MainViewModel(new WordListRepository(), new TestProgress(), settings, new ReviewSession(), new TestCatalog(), new TestPlayer(), _ => { }))
            {
                vm.SelectFolder(folder); vm.SelectFolder(emptyFolder);
                Equal(0, vm.WordLists.Count); Equal<Word?>(null, vm.CurrentWord);
                Equal(emptyFolder, settings.Load().Folder); Equal("", settings.Load().WordListFile);
            }
            using var reopened = new MainViewModel(new WordListRepository(), new TestProgress(), settings, new ReviewSession(), new TestCatalog(), new TestPlayer(), _ => { });
            reopened.Initialize(folder); Equal(0, reopened.WordLists.Count); Equal(emptyFolder, reopened.Settings.Folder);
        });
        Check("invalid wordlist warning cannot mask corrupt progress", () => {
            string errorFolder = Path.Combine(Temp, "opening-errors"); Directory.CreateDirectory(errorFolder);
            string path = Path.Combine(errorFolder, "valid.json"); File.WriteAllText(path, Minimal);
            File.WriteAllText(Path.Combine(errorFolder, "bad.json"), "{}");
            string progressFile = ProgressRepository.ProgressPath(path); Directory.CreateDirectory(Path.GetDirectoryName(progressFile)!);
            File.WriteAllText(progressFile, "{broken");
            using var vm = new MainViewModel(new WordListRepository(), new ProgressRepository(), new SettingsRepository(Path.Combine(Temp, "opening-errors.tmp")), new ReviewSession(), new TestCatalog(), new TestPlayer(), _ => { });
            vm.SelectFolder(errorFolder);
            Equal(true, vm.Warning.Contains("学习记录无法读取")); Equal(false, vm.UnknownCommand.CanExecute(null));
            Equal("{broken", File.ReadAllText(progressFile)); Equal("aware", vm.CurrentWord!.Text);
        });
        Check("filter counts update after rating, rerating, failure and reload", () => {
            var progress = new TestProgress();
            using var vm = new MainViewModel(new WordListRepository(), progress, new SettingsRepository(Path.Combine(folder, "counts.tmp")), new ReviewSession(), new TestCatalog(), new TestPlayer(), _ => { });
            vm.SelectFolder(folder);
            Equal("全部(2)", vm.Filters[0].Label); Equal("未学(2)", vm.Filters[1].Label);
            int changed = 0; vm.Filters[2].PropertyChanged += (_, _) => changed++;
            vm.UnknownCommand.Execute(null);
            Equal("未学(1)", vm.Filters[1].Label); Equal("不懂(1)", vm.Filters[2].Label); Equal(1, changed);
            vm.Navigate(-1); vm.FamiliarCommand.Execute(null);
            Equal("不懂(0)", vm.Filters[2].Label); Equal("认识(1)", vm.Filters[3].Label);
            vm.Navigate(-1); vm.MasteredCommand.Execute(null);
            Equal("认识(0)", vm.Filters[3].Label); Equal("掌握(1)", vm.Filters[4].Label);
            vm.SelectedFilter = ReviewFilter.Unseen;
            Equal("b", vm.CurrentWord!.Text);
            Equal(ReviewFilter.Unseen, vm.SelectedFilter); Equal("全部(2)", vm.Filters[0].Label);
            progress.FailSave = true; vm.Rate(StudyLevel.Unknown);
            Equal("未学(1)", vm.Filters[1].Label); Equal("不懂(0)", vm.Filters[2].Label);
            vm.SelectedFilter = ReviewFilter.Mastered;
            Equal(ReviewFilter.Mastered, vm.SelectedFilter); Equal("a", vm.CurrentWord!.Text);
            progress.FailSave = false; vm.OpenWordList(vm.SelectedWordList!);
            Equal("未学(2)", vm.Filters[1].Label); Equal("掌握(0)", vm.Filters[4].Label);
        });
        Check("display queues UK once, reveal does not replay", () => {
            var player = new TestPlayer(); var queue = new List<Action>();
            var vm = new MainViewModel(new WordListRepository(), new TestProgress(), new SettingsRepository(Path.Combine(folder, "settings.tmp")), new ReviewSession(), new TestCatalog(), player, action => queue.Add(action));
            vm.SelectFolder(folder); queue[0](); Equal("a-uk.mp3", player.Played[0]);
            vm.RevealDefinition(); Equal(true, vm.DefinitionVisible); Equal(1, queue.Count);
            vm.RevealCommand.Execute(null); Equal(false, vm.DefinitionVisible); Equal(1, queue.Count);
            Equal<Word?>(null, vm.DefinitionWord);
            vm.RevealCommand.Execute(null); Equal(true, vm.DefinitionVisible); Equal(1, queue.Count);
            Equal(vm.CurrentWord, vm.DefinitionWord);
            vm.Rate(StudyLevel.Familiar); Equal("b", vm.CurrentWord!.Text); Equal(false, vm.DefinitionVisible);
        });
        Check("save failure restores original word and no next audio", () => {
            var player = new TestPlayer(); var queue = new List<Action>(); var progress = new TestProgress();
            var vm = new MainViewModel(new WordListRepository(), progress, new SettingsRepository(Path.Combine(folder, "settings.tmp")), new ReviewSession(), new TestCatalog(), player, action => queue.Add(action));
            vm.SelectFolder(folder); queue[0](); progress.FailSave = true; vm.Rate(StudyLevel.Mastered);
            Equal("a", vm.CurrentWord!.Text); Equal(1, queue.Count); Equal(true, vm.Warning.Contains("test locked"));
        });
        Check("rapid navigation cancels stale automatic playback", () => {
            var player = new TestPlayer(); var queue = new List<Action>();
            var vm = new MainViewModel(new WordListRepository(), new TestProgress(), new SettingsRepository(Path.Combine(folder, "settings.tmp")), new ReviewSession(), new TestCatalog(), player, action => queue.Add(action));
            vm.SelectFolder(folder); vm.Navigate(1); queue[0](); queue[1](); Equal(1, player.Played.Count); Equal("b-uk.mp3", player.Played[0]);
        });
        Check("navigation and filters never save progress", () => {
            var progress = new TestProgress();
            using var vm = new MainViewModel(new WordListRepository(), progress, new SettingsRepository(Path.Combine(folder, "navigation.tmp")), new ReviewSession(), new TestCatalog(), new TestPlayer(), _ => { });
            vm.SelectFolder(folder); vm.Navigate(1); vm.Navigate(-1); vm.SelectedFilter = ReviewFilter.Unseen;
            Equal(0, progress.Saves); vm.Rate(StudyLevel.Unknown); Equal(1, progress.Saves);
        });
    }
}
