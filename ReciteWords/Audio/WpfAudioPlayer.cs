using System.Windows.Media;
namespace ReciteWords.Audio;

public class WpfAudioPlayer : IAudioPlayer
{
    private readonly MediaPlayer player = new MediaPlayer();
    public event EventHandler<string>? PlaybackFailed;
    public WpfAudioPlayer()
    {
        player.MediaFailed += OnFailed;
        player.MediaEnded += OnEnded;
    }
    public void Play(string path)
    {
        Stop();
        try { player.Open(new Uri(Path.GetFullPath(path))); player.Play(); }
        catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is ArgumentException)
        { PlaybackFailed?.Invoke(this, ex.Message); }
    }
    public void Stop() { player.Close(); }
    private void OnFailed(object? sender, ExceptionEventArgs e) { PlaybackFailed?.Invoke(this, e.ErrorException.Message); }
    private void OnEnded(object? sender, EventArgs e) { player.Close(); }
    public void Dispose() { player.MediaFailed -= OnFailed; player.MediaEnded -= OnEnded; player.Close(); }
}
