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
        public AudioLoadResult Load(string path) => new AudioLoadResult();
        public string? FindWord(string word, string accent) => accent == "uk" ? word + "-uk.mp3" : word + "-us.mp3";
        public string? FindExample(string eid, string accent) => null;
    }
    class TestProgress : IProgressRepository
    {
        public bool FailSave;
        public ReviewProgress LoadOrCreate(string path, WordList words)
        {
            var result = new ReviewProgress(); foreach (var word in words.Words) result.Session.Queue.Add(word.Wid); return result;
        }
        public void Save(string path, ReviewProgress progress) { if (FailSave) throw new IOException("test locked"); }
    }
    static void ViewModelTests()
    {
        string folder = Path.Combine(Temp, "vm"); Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "sample.json"), "{\"words\":[{\"wid\":\"a\",\"word\":\"a\",\"phonetic\":\"\",\"senses\":[{\"chinese_meaning\":\"甲\"}]},{\"wid\":\"b\",\"word\":\"b\",\"phonetic\":\"\",\"senses\":[{\"chinese_meaning\":\"乙\"}]}]}");
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
            Equal("b", vm.CurrentWord!.Wid);
            Equal(ReviewFilter.Unseen, vm.SelectedFilter); Equal("全部(2)", vm.Filters[0].Label);
            progress.FailSave = true; vm.Rate(StudyLevel.Unknown);
            Equal("未学(1)", vm.Filters[1].Label); Equal("不懂(0)", vm.Filters[2].Label);
            vm.SelectedFilter = ReviewFilter.Mastered;
            Equal(ReviewFilter.Unseen, vm.SelectedFilter); Equal("b", vm.CurrentWord!.Wid);
            progress.FailSave = false; vm.OpenWordList(vm.SelectedWordList!);
            Equal("未学(2)", vm.Filters[1].Label); Equal("掌握(0)", vm.Filters[4].Label);
        });
        Check("display queues UK once, reveal does not replay", () => {
            var player = new TestPlayer(); var queue = new List<Action>();
            var vm = new MainViewModel(new WordListRepository(), new TestProgress(), new SettingsRepository(Path.Combine(folder, "settings.tmp")), new ReviewSession(), new TestCatalog(), player, action => queue.Add(action));
            vm.SelectFolder(folder); queue[0](); Equal("a-uk.mp3", player.Played[0]);
            vm.RevealDefinition(); Equal(true, vm.DefinitionVisible); Equal(1, queue.Count);
            vm.Rate(StudyLevel.Familiar); Equal("b", vm.CurrentWord!.Wid); Equal(false, vm.DefinitionVisible);
        });
        Check("save failure restores original word and no next audio", () => {
            var player = new TestPlayer(); var queue = new List<Action>(); var progress = new TestProgress();
            var vm = new MainViewModel(new WordListRepository(), progress, new SettingsRepository(Path.Combine(folder, "settings.tmp")), new ReviewSession(), new TestCatalog(), player, action => queue.Add(action));
            vm.SelectFolder(folder); queue[0](); progress.FailSave = true; vm.Rate(StudyLevel.Mastered);
            Equal("a", vm.CurrentWord!.Wid); Equal(1, queue.Count); Equal(true, vm.Warning.Contains("test locked"));
        });
        Check("rapid navigation cancels stale automatic playback", () => {
            var player = new TestPlayer(); var queue = new List<Action>();
            var vm = new MainViewModel(new WordListRepository(), new TestProgress(), new SettingsRepository(Path.Combine(folder, "settings.tmp")), new ReviewSession(), new TestCatalog(), player, action => queue.Add(action));
            vm.SelectFolder(folder); vm.Navigate(1); queue[0](); queue[1](); Equal(1, player.Played.Count); Equal("b-uk.mp3", player.Played[0]);
        });
    }
}
