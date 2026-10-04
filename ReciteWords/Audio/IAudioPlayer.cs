namespace ReciteWords.Audio;

public interface IAudioPlayer : IDisposable
{
    void Play(string path);
    void Stop();
    event EventHandler<string>? PlaybackFailed;
}
