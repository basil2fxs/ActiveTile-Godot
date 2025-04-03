using Godot;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using System;
using System.Collections.Generic;

namespace MysticClue.Chroma.GodotClient.Games;

/// <summary>
/// Zen game where dots on the floor move around randomly.
/// </summary>
public partial class GridRandomWalkGame : GridGame
{
    public static IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs)
    {
        for (int playerCount = 1; playerCount <= maxSupportedPlayers; playerCount++)
            yield return new(
                playerCount,
                GameSelection.GameType.Zen,
                GameSelection.GameDifficulty.Newbie,
                "RandomWalk",
                null,
                "Relax. Walk around.",
                GD.Load<Texture2D>("res://HowToPlay/random-walk.jpg"),
                randomSeed => new GridRandomWalkGame(gridSpecs, randomSeed));
    }

    Random _rand;
    List<Polygon2D> _polygons = [];
    bool[,] _spaceOccupied;

    public GridRandomWalkGame(ResolvedGridSpecs grid, int randomSeed) : base(grid)
    {
        GDPrint.LogState(randomSeed);

        _rand = new Random(randomSeed);

        var width = GridSpecs.Width;
        var height = GridSpecs.Height;

        _spaceOccupied = new bool[width, height];
    }

    public override void _Process(double delta)
    {
        foreach (var (x, y) in TakePressed())
        {
            if (!_spaceOccupied[x, y])
            {
                var p = MakePolygon(new(), Vector2.One, Color.FromOkHsl(_rand.NextSingle(), 0.9f, 0.5f));
                p.Position = new(x, y);
                var t = new Timer()
                {
                    Autostart = true,
                    WaitTime = 0.5 + _rand.NextSingle(),
                };
                t.Timeout += () =>
                {
                    var x = (int)p.Position.X;
                    var y = (int)p.Position.Y;
                    List<(int, int)> directions = [];
                    void tryAdd(int x, int y)
                    {
                        if (!IsSpaceInGrid(x, y) || !_spaceOccupied[x, y]) directions.Add((x, y));
                    }
                    tryAdd(x - 1, y);
                    tryAdd(x + 1, y);
                    tryAdd(x, y - 1);
                    tryAdd(x, y + 1);
                    if (directions.Count == 0)
                    {
                        FlashColor(p, p.Color, Colors.White, p.Color);
                    }
                    else
                    {
                        var (nextX, nextY) = directions[_rand.Next(0, directions.Count)];
                        var tween = p.CreateTween();
                        var setPosition = Callable.From((Vector2 position) => p.Position = position);
                        tween.TweenMethod(setPosition, p.Position, new Vector2(nextX, nextY), 0.3f + 0.1 * t.WaitTime);
                        tween.TweenCallback(Callable.From(() => _spaceOccupied[x, y] = false));
                        if (IsSpaceInGrid(nextX, nextY)) _spaceOccupied[nextX, nextY] = true;
                        else tween.TweenCallback(Callable.From(p.QueueFree));
                    }

                };
                p.AddChild(t);
                AddChild(p);
                _spaceOccupied[x, y] = true;
            }
        }
    }

    private bool IsSpaceInGrid(int x, int y) =>
        x >= 0 && y >= 0 && x < _spaceOccupied.GetLength(0) && y < _spaceOccupied.GetLength(1);
}
