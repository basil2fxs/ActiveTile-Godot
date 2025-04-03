using Godot;

namespace MysticClue.Chroma.GodotClient;

/// <summary>
/// Interface a view into the rendered game for debugging.
/// </summary>
public interface IGameView
{
    /// <summary>
    /// The actual pixels that should be pushed to hardware.
    /// </summary>
    public SubViewport HardwareView { get; }

    /// <summary>
    /// Internal render view, if different from HardwareView.
    /// </summary>
    public SubViewport FullResolutionView { get; }

    /// <summary>
    /// Handle clicks from the debug view by simulating hardware input.
    /// </summary>
    /// <param name="position">Position in game output viewport coordinates.</param>
    /// <param name="pressed">Whether it's a mousedown.</param>
    public void DebugInput(Vector2 position, bool pressed);
}
