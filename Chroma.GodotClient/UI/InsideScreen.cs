using Godot;

namespace MysticClue.Chroma.GodotClient.UI;

/// <summary>
/// Manages common UI elements in the inside screen.
///
/// Elements are hidden by default.
///
/// Put any custom elements into PerGameContainer.
/// </summary>
public partial class InsideScreen : Control
{
	public TimeDisplay TimeDisplay => GetNode<TimeDisplay>("%TimeDisplay")!;

	public LifeCounter LifeCounter => GetNode<LifeCounter>("%LifeCounter")!;

	public ScoreCounter ScoreCounter => GetNode<ScoreCounter>("%ScoreCounter")!;

	public Label Message => GetNode<Label>("%Message")!;

	public Control PerGameContainer => GetNode<Control>("%PerGameContainer")!;

	public void ClearGame()
	{
		TimeDisplay.Update(0);
		TimeDisplay.Hide();
		LifeCounter.Update(0, 0);
		LifeCounter.Hide();
		ScoreCounter.Update(0);
		ScoreCounter.Hide();
		Message.Text = "";
		foreach (var c in PerGameContainer.GetChildren()) { c.QueueFree(); }
	}

	public override void _Ready()
	{
		ClearGame();
		TimeDisplay.AddThemeFontSizeOverride("font_size", 100);
		LifeCounter.AddThemeFontSizeOverride("font_size", 100);
		ScoreCounter.AddThemeFontSizeOverride("font_size", 100);
		Message.AddThemeFontSizeOverride("font_size", 80);
	}
}
