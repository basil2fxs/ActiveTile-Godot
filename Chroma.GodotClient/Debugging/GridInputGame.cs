using Godot;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.Games;

namespace MysticClue.Chroma.GodotClient.Debugging;

/// <summary>
/// Test game to check that inputs are being received.
/// </summary>
public partial class GridInputGame : GridGame
{
    // Containers/layers for game objects.
    Node2D _mainLayer;

    public GridInputGame(ResolvedGridSpecs grid) : base(grid)
    {
        _mainLayer = new Node2D();
        _mainLayer.Name = "MainLayer";

        for (var x = 0; x < grid.Width; ++x)
        {
            for (var y = 0; y < grid.Height; ++y)
            {
                var t = MakePolygonArea(new(1, 1), Colors.Black);
                t.Position = new(x, y);
                _mainLayer.AddChild(t);
            }
        }

        AddChild(_mainLayer);
    }

    public override void _PhysicsProcess(double delta)
    {
        var spaceState = GetViewport().World2D.DirectSpaceState;

        for (var x = 0; x < GridSpecs.Width; ++x)
        {
            for (var y = 0; y < GridSpecs.Height; ++y)
            {
                // Check for collision at the center of this tile.
                var v = new Vector2(x + 0.5f, y + 0.5f);
                var result = spaceState.IntersectPoint(new()
                {
                    CollideWithAreas = true,
                    CollideWithBodies = false,
                    Position = v
                });
                foreach (var r in result)
                {
                    var collider = r["collider"].As<CollisionObject2D>();
                    var parent = collider.GetParent();
                    if (parent == _mainLayer) {
                        foreach (Polygon2D p in collider.GetChildrenByType<Polygon2D>())
                        {
                            p.Color = IsPressed(x, y) ? Colors.White : Colors.Black;
                        }
                    }
                }
            }
        }

    }
}
