using Godot;

namespace MysticClue.Chroma.GodotClient;

/// <summary>
/// A normally-hidden window to visualize what's being sent to the hardware.
/// </summary>
public partial class HardwareView : Window
{
	private IGameView? _game;
	private bool _showFullResolution;

	public void SetShowFullResolution(bool value)
	{
		var changed = value != _showFullResolution;
		_showFullResolution = value;
		if (changed && _game != null) { SetGame(_game); }
	}

	public override void _Ready()
	{
		SizeChanged += UpdateCamera;
	}

	private SubViewport? _viewport => _showFullResolution ? _game?.FullResolutionView : _game?.HardwareView;

	public void UpdateCamera()
	{
		if (_viewport == null) { return; }

		var viewportSize = _viewport.Size.ToVector2();
		var camera = GetNode<Camera2D>("HardwareViewCamera");
		var zoom = float.Min(Size.X / viewportSize.X, Size.Y / viewportSize.Y);
		camera.Zoom = new(zoom, zoom);
	}

	public void SetGame(IGameView game)
	{
		_game = game;
		GetNode<Sprite2D>("GameOutputSprite").Texture = _viewport?.GetTexture();
		UpdateCamera();
	}

	public override void _Input(InputEvent @event)
	{
		Vector2 convertToGameCoordinates(Vector2 position)
		{
			// Since we center the grid on the screen, convert the coordinates to have origin at center.
			var p = (position - 0.5f * Size.ToVector2());
			// Then convert to game output coordinates.
			var gameSize = _game!.HardwareView.Size.ToVector2();
			var zoom = float.Min(Size.X / gameSize.X, Size.Y / gameSize.Y);
			p = p / zoom + 0.5f * gameSize;
			return p;
		}

		if (_game == null) return;

		if (@event is InputEventMouseButton button)
		{
			if (button.ButtonIndex == MouseButton.Left)
			{
				_game.DebugInput(convertToGameCoordinates(button.Position), button.Pressed);
			}
		}
		if (@event is InputEventMouseMotion motion && (motion.ButtonMask & MouseButtonMask.Left) > 0)
		{
			var previous = convertToGameCoordinates(motion.Position - motion.Relative);
			var current = convertToGameCoordinates(motion.Position);
			if (previous != current)
			{
				_game.DebugInput(previous, false);
				_game.DebugInput(current, true);
			}
		}
	}
}
