using Godot;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;

namespace MysticClue.Chroma.GodotClient.Audio;

public partial class FadingAudioStreamPlayer : AudioStreamPlayer
{
    // Fade rate in dB per second. Negative if fading out.
    public double FadeRate { get; set; }
    public float TargetVolumeDb { get; set; }

    private const float MinimumVolumeDb = -80;

    public void FadeOut(double seconds)
    {
        TargetVolumeDb = MinimumVolumeDb;
        if (seconds <= 0)
        {
            VolumeDb = MinimumVolumeDb;
            FadeRate = 0;
            return;
        }
        FadeRate = MinimumVolumeDb / seconds;
    }

    public void FadeIn(double seconds, float targetVolumeDb)
    {
        Assert.Clamp(ref targetVolumeDb, MinimumVolumeDb, 0);
        TargetVolumeDb = targetVolumeDb;
        if (seconds <= 0)
        {
            VolumeDb = targetVolumeDb;
            FadeRate = 0;
            return;
        }
        FadeRate = (targetVolumeDb - MinimumVolumeDb) / seconds;

        if (!Playing)
        {
            VolumeDb = MinimumVolumeDb;
            Play();
        }
    }

    public void AbruptPlay(float volumeDb)
    {
        FadeRate = 0;
        TargetVolumeDb = volumeDb;
        VolumeDb = volumeDb;
        Play();
    }

    public void AbruptStop()
    {
        Stop();
    }

    public override void _Process(double delta)
    {
        if (Playing)
        {
            if (FadeRate > 0)
            {
                if (VolumeDb < TargetVolumeDb)
                    VolumeDb += (float)double.Min(FadeRate * delta, TargetVolumeDb - VolumeDb);
            }
            else if (FadeRate < 0)
            {
                if (VolumeDb > TargetVolumeDb)
                    VolumeDb += (float)double.Max(FadeRate * delta, TargetVolumeDb - VolumeDb);
                else
                    Stop();
            }

        }

        base._Process(delta);
    }
}
