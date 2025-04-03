using Godot;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.Games;

namespace MysticClue.Chroma.GodotClient.Debugging;

/// <summary>
/// Feature to turn on all lights for cleaning etc.
/// </summary>
public partial class GridLightGame : GridGame
{
    public bool On { get; set; } = true;

    [Signal]
    public delegate void LightOffEventHandler();

    Color _color = new(1, 1, 1, 0);

    public GridLightGame(ResolvedGridSpecs grid) : base(grid)
    {
        var colorRect = new ColorRect() { SizeFlagsVertical = Control.SizeFlags.ExpandFill, Color = _color };
        InsideScreen!.PerGameContainer.AddChild(colorRect);

        var background = MakePolygon(Vector2.Zero, new(GridSpecs.Width, GridSpecs.Height), _color);
        background.Name = "Background";
        AddChild(background);
    }

    public override void _Process(double delta)
    {
        var target = On ? 1f : 0f;
        if (_color.A != target)
        {
            _color.A = float.Clamp(_color.A + (target - 0.5f) * 2f * (float)delta, 0, 1);
            if (_color.A == 0) EmitSignal(SignalName.LightOff);
            InsideScreen!.PerGameContainer.GetChild<ColorRect>(0).Color = _color;
            GetChild<Polygon2D>(0).Color = _color;
        }
    }
}
