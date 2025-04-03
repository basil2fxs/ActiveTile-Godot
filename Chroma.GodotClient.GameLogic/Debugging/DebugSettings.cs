namespace MysticClue.Chroma.GodotClient.GameLogic.Debugging;

/// <summary>
/// Default settings for debug options.
/// </summary>
/// <param name="ShowHardwareView">Show a window visualizing what's sent to the hardware.</param>
/// <param name="ShowFullResolution">Show the full HardwareView render resolution, rather than the scaled down final frame.</param>
/// <param name="SkipIntro">Set SkipIntro on games so they get to the play state faster.</param>
/// <param name="SeparateInsideScreen">Put the inside screen view on a separate window.</param>
/// <param name="FullScreen">Whether to take up the full screen (or multiple if <paramref name="SeparateInsideScreen"/>).</param>
public record struct DebugSettings(
    bool ShowHardwareView,
    bool ShowFullResolution,
    bool SkipIntro,
    bool SeparateInsideScreen,
    bool FullScreen);
