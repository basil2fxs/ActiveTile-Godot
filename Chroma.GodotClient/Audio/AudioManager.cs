using Godot;
using Godot.Collections;
using System.Diagnostics;

namespace MysticClue.Chroma.GodotClient.Audio;

public partial class AudioManager : Node
{
    private Dictionary<Sounds, AudioStream> _streams = new();

    private FadingAudioStreamPlayer[] _musicPlayer;
    private int _currentMusic;
    private FadingAudioStreamPlayer[] _sfxPlayer;

    // Always fade sounds when stopping abruptly.
    private const double MinimumFadeTime = 0.1;

    public AudioManager()
    {
        _musicPlayer = new FadingAudioStreamPlayer[2];
        for (int i = 0; i < _musicPlayer.Length; ++i) { _musicPlayer[i] = new() { Bus = "Music" }; }
        _sfxPlayer = new FadingAudioStreamPlayer[32];
        for (int i = 0; i < _sfxPlayer.Length; ++i) { _sfxPlayer[i] = new() { Bus = "Sfx" }; }
    }

    public override void _Ready()
    {
        foreach (var p in _musicPlayer) { AddChild(p); }
        foreach (var p in _sfxPlayer) { AddChild(p); }
    }

    public void Load(Sounds sound)
    {
        if (!_streams.ContainsKey(sound))
        {
            _streams[sound] = GD.Load<AudioStream>("Audio Files/" + SoundFiles.Path[sound]);
        }
    }

    public void StopAll(double fadeTime = MinimumFadeTime)
    {
        foreach (var p in _musicPlayer) { p.FadeOut(fadeTime); }
        foreach (var p in _sfxPlayer) { p.FadeOut(fadeTime); }
    }

    /// <summary>
    /// Play a sound as background music.
    ///
    /// Only one music stream is active at a time.
    /// </summary>
    public FadingAudioStreamPlayer PlayMusic(Sounds sound, double fadeTime = 1, float volumeDb = 0, bool loop = true)
    {
        _musicPlayer[_currentMusic].FadeOut(fadeTime);
        _currentMusic ^= 1;
        var mp = _musicPlayer[_currentMusic];
        mp.AbruptStop();
        mp.Stream = _streams[sound];
        mp.FadeIn(fadeTime, volumeDb);

        if (loop)
            mp.Finished += () => PlayMusic(sound, fadeTime, volumeDb, loop);

        return mp;
    }

    public void StopMusic(double fadeTime = 1)
    {
        _musicPlayer[_currentMusic].FadeOut(fadeTime);
    }

    public Playback? Play(Sounds sound, float pitchScale = 1)
    {
        for (int i = 0; i < _sfxPlayer.Length; ++i)
        {
            var p = _sfxPlayer[i];
            if (!p.Playing)
            {
                p.Stream = _streams[sound];
                p.PitchScale = pitchScale;
                p.AbruptPlay(0);
                return new Playback(i);
            }
        }
        return null;
    }

    public void Stop(Playback playback, double fadeTime = MinimumFadeTime)
    {
        _sfxPlayer[playback.Sfx].FadeOut(fadeTime);
    }

    public record struct Playback(int Sfx);
}
