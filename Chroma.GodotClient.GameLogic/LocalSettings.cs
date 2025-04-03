using MysticClue.Chroma.GodotClient.GameLogic.Debugging;

namespace MysticClue.Chroma.GodotClient.GameLogic;

/// <summary>
/// Settings for the software specific to this machine.
///
/// Normally stored and read as a JSON file.
/// </summary>
public class LocalSettings
{
    /// <summary>
    /// Default debug settings.
    /// </summary>
    public DebugSettings? DebugSettings { get; set; }
}
