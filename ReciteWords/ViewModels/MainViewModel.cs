using ReciteWords.Models;
using ReciteWords.Storage;
using ReciteWords.Review;
using ReciteWords.Audio;
using System.ComponentModel;
using System.Text.Json;
namespace ReciteWords.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IWordListRepository words;
    private readonly IProgressRepository progress;
    private readonly ISettingsRepository settings;
    private readonly IReviewSession session;
    private readonly IAudioCatalog catalog;
    private readonly IAudioPlayer player;
    private readonly Action<Action> queuePlayback;
    private readonly List<SimpleCommand> commands = new List<SimpleCommand>();
    private WordListEntry? selectedWordList;
    private ReviewFilter selectedFilter;
    private bool progressWritable;
    private bool loaded;
    private bool disposed;
    private int playbackRequest;
    private string warning = "";
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? FolderRequested;
    public event EventHandler? WarningChanged;
    public AppSettings Settings { get; private set; } = new AppSettings();
    public List<WordListEntry> WordLists { get; private set; } = new List<WordListEntry>();
    public List<FilterChoice> Filters { get; } = new List<FilterChoice>() {
        new FilterChoice(ReviewFilter.All, "全部"), new FilterChoice(ReviewFilter.Unseen, "未学"), new FilterChoice(ReviewFilter.Unknown, "不懂"), new FilterChoice(ReviewFilter.Familiar, "认识"), new FilterChoice(ReviewFilter.Mastered, "掌握")
    };
    public SimpleCommand FolderCommand { get; }
    public SimpleCommand PreviousCommand { get; }
    public SimpleCommand NextCommand { get; }
    public SimpleCommand RevealCommand { get; }
    public SimpleCommand UnknownCommand { get; }
    public SimpleCommand FamiliarCommand { get; }
    public SimpleCommand MasteredCommand { get; }
    public SimpleCommand UkCommand { get; }
    public SimpleCommand UsCommand { get; }
    public MainViewModel(IWordListRepository words, IProgressRepository progress, ISettingsRepository settings, IReviewSession session, IAudioCatalog catalog, IAudioPlayer player, Action<Action> queuePlayback)
    {
        this.words = words;
        this.progress = progress;
        this.settings = settings;
        this.session = session;
        this.catalog = catalog;
        this.player = player;
        this.queuePlayback = queuePlayback;
        FolderCommand = Command(() => FolderRequested?.Invoke(this, EventArgs.Empty));
        PreviousCommand = Command(() => Navigate(-1), () => loaded && progressWritable && session.CanMovePrevious);
        NextCommand = Command(() => Navigate(1), () => loaded && progressWritable && session.CanMoveNext);
        RevealCommand = Command(RevealDefinition, () => CurrentWord != null);
        UnknownCommand = Command(() => Rate(StudyLevel.Unknown), CanRate);
        FamiliarCommand = Command(() => Rate(StudyLevel.Familiar), CanRate);
        MasteredCommand = Command(() => Rate(StudyLevel.Mastered), CanRate);
        UkCommand = Command(() => PlayWord("uk"), () => WordAudio("uk") != null);
        UsCommand = Command(() => PlayWord("us"), () => WordAudio("us") != null);
        player.PlaybackFailed += OnPlaybackFailed;
        try { Settings = settings.Load(); }
        catch (Exception ex) when (FileError(ex)) { Warn("设置读取失败，使用默认设置：" + ex.Message); }
    }
    private SimpleCommand Command(Action action, Func<bool>? enabled = null)
    {
        var command = new SimpleCommand(action, enabled); commands.Add(command); return command;
    }
    public void Initialize(string defaultFolder)
    {
        string folder = Settings.Folder;
        string? missing = null;
        if (folder.Length == 0) folder = defaultFolder;
        else if (!Directory.Exists(folder)) { missing = "上次文件夹不存在，已回到默认目录。"; folder = defaultFolder; }
        SelectFolder(folder);
        if (missing != null) Warn(missing);
    }
    public WordListEntry? SelectedWordList
    {
        get => selectedWordList;
        set { if (value != null && !ReferenceEquals(value, selectedWordList)) OpenWordList(value); }
    }
    public ReviewFilter SelectedFilter
    {
        get => selectedFilter;
        set
        {
            if (value == selectedFilter) return;
            if (loaded && progressWritable) { session.Start(value); PublishCurrent(); }
            else
            {
                if (!loaded) selectedFilter = value;
                Notify();
            }
        }
    }
    public Word? CurrentWord => loaded ? session.CurrentWord : null;
    public bool DefinitionVisible { get; private set; }
    public Word? DefinitionWord => DefinitionVisible ? CurrentWord : null;
    public bool HasWarning => warning.Length > 0;
    public string Warning => warning;
    public string InformationText => warning.Replace("\r", "").Replace("\n", " · ");
    public string WordHeading
    {
        get
        {
            if (CurrentWord != null) return CurrentWord.Text;
            if (!loaded) return "请选择单词本文件夹";
            if (session.Count == 0) return "此分类没有单词";
            return "本轮复习完成";
        }
    }
    public string phonetic_uk
    {
        get
        {
            if (CurrentWord == null) return "";
            if (CurrentWord.phonetic_uk.Length == 0) return "—";
            return CurrentWord.phonetic_uk;
        }
    }
    public string phonetic_us
    {
        get
        {
            if (CurrentWord == null) return "";
            if (CurrentWord.phonetic_us.Length == 0) return "—";
            return CurrentWord.phonetic_us;
        }
    }
    public void SelectFolder(string folder)
    {
        ScanResult result;
        try { result = words.Scan(folder); }
        catch (Exception ex) when (FileError(ex)) { Warn("无法读取文件夹：" + ex.Message); return; }
        player.Stop();
        playbackRequest++;
        loaded = false;
        progressWritable = false;
        DefinitionVisible = false;
        selectedWordList = null;
        WordLists = result.Entries;
        RefreshFilterCounts();
        Notify();
        if (WordLists.Count > 0)
        {
            if (result.Errors.Count > 0) Warn("已跳过无效文件：\n" + string.Join("\n", result.Errors));
            WordListEntry choice = WordLists[0];
            if (string.Equals(folder, Settings.Folder, StringComparison.OrdinalIgnoreCase))
                foreach (var entry in WordLists) if (Path.GetFileName(entry.Path) == Settings.WordListFile) choice = entry;
            OpenWordList(choice);
        }
        else
        {
            Settings.Folder = Path.GetFullPath(folder);
            Settings.WordListFile = "";
            Warn("当前文件夹中没有有效单词本。" + (result.Errors.Count > 0 ? "\n" + string.Join("\n", result.Errors) : ""));
            SaveSettings();
        }
    }
    public void OpenWordList(WordListEntry entry)
    {
        WordList wordList;
        try { wordList = words.Load(entry.Path); }
        catch (Exception ex) when (FileError(ex)) { Warn("单词本读取失败：" + ex.Message); Notify(); return; }
        selectedWordList = entry;
        ReviewProgress data;
        string? progressError = null;
        try { data = progress.LoadOrCreate(entry.Path); progressWritable = true; }
        catch (Exception ex) when (FileError(ex))
        { data = new ReviewProgress(); progressWritable = false; progressError = "学习记录无法读取或初始化，请自行检查对应 userdata 文件：" + ex.Message; }
        session.Load(wordList, data);
        loaded = true;
        catalog.Load(entry.Path, wordList);
        Settings.Folder = Path.GetDirectoryName(Path.GetFullPath(entry.Path))!;
        Settings.WordListFile = Path.GetFileName(entry.Path);
        SaveSettings();
        PublishCurrent();
        if (progressError != null) Warn(progressError);
    }
    private bool CanRate() => loaded && progressWritable && CurrentWord != null;
    public void Navigate(int offset)
    {
        if (!loaded || !progressWritable || (offset == -1 && !session.CanMovePrevious) || (offset == 1 && !session.CanMoveNext)) return;
        session.Move(offset); PublishCurrent();
    }
    public void Rate(StudyLevel level)
    {
        if (!CanRate()) return;
        var before = session.Capture();
        session.Rate(level);
        try { progress.Save(selectedWordList!.Path, session.GetProgress()); }
        catch (Exception ex) when (FileError(ex))
        {
            session.Restore(before);
            Warn("学习记录保存失败，操作未提交：" + ex.Message);
            return;
        }
        PublishCurrent();
    }
    private void PublishCurrent()
    {
        player.Stop();
        int request = ++playbackRequest;
        selectedFilter = session.Filter;
        DefinitionVisible = false;
        RefreshFilterCounts();
        Notify();
        string? path = WordAudio("uk");
        if (path != null) queuePlayback(() => { if (!disposed && request == playbackRequest) player.Play(path); });
    }
    public void RevealDefinition() { if (CurrentWord != null) { DefinitionVisible = !DefinitionVisible; Notify(); } }
    public string? WordAudio(string accent) => CurrentWord == null ? null : catalog.FindWord(CurrentWord.Text, accent);
    public string? ExampleAudio(string eid, string accent) => CurrentWord == null || eid.Length == 0 ? null : catalog.FindExample(CurrentWord.Text, eid, accent);
    public void PlayWord(string accent) { PlayPath(WordAudio(accent)); }
    public void PlayExample(string eid, string accent) { PlayPath(ExampleAudio(eid, accent)); }
    private void PlayPath(string? path) { if (path != null) { playbackRequest++; player.Play(path); } }
    private void OnPlaybackFailed(object? sender, string message) { Warn("本地音频播放失败：" + message); }
    public void SaveSettings()
    {
        try { settings.Save(Settings); }
        catch (Exception ex) when (FileError(ex)) { Warn("设置保存失败：" + ex.Message); }
    }
    public void Warn(string message) { warning = message; Notify(); WarningChanged?.Invoke(this, EventArgs.Empty); }
    public void ClearWarning() { warning = ""; Notify(); }
    private void RefreshFilterCounts()
    {
        foreach (var filter in Filters) filter.UpdateCount(loaded ? session.GetCount(filter.Value) : 0);
    }
    private void Notify() { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); foreach (var command in commands) command.Refresh(); }
    private static bool FileError(Exception ex) => ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is JsonException;
    public void Dispose() { disposed = true; playbackRequest++; player.PlaybackFailed -= OnPlaybackFailed; player.Dispose(); }
}
