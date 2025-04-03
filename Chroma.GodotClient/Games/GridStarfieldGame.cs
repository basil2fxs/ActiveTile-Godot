using Godot;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.GameLogic.ProcGen;
using System;
using System.Collections.Generic;

namespace MysticClue.Chroma.GodotClient.Games;

/// <summary>
/// Zen game where the floor lights up randomly.
/// </summary>
public partial class GridStarfieldGame : GridGame
{
    public static IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs)
    {
        for (int playerCount = 1; playerCount <= maxSupportedPlayers; playerCount++)
            yield return new(
                playerCount,
                GameSelection.GameType.Zen,
                GameSelection.GameDifficulty.Newbie,
                "Starfield",
                null,
                "Relax. Watch the stars or make more with your steps.",
                GD.Load<Texture2D>("res://HowToPlay/space-11099_1920.jpg"),
                randomSeed => new GridStarfieldGame(gridSpecs, randomSeed));
    }

    Random _rand;
    long _simplexSeedA;
    long _simplexSeedB;
    Polygon2D[,] _polygons;
    Pulse[,] _pulses;
    float[,] _pressedFactor;

    public GridStarfieldGame(ResolvedGridSpecs grid, int randomSeed) : base(grid)
    {
        GDPrint.LogState(randomSeed);

        _rand = new Random(randomSeed);
        _simplexSeedA = _rand.NextInt64();
        _simplexSeedB = _rand.NextInt64();

        var width = GridSpecs.Width;
        var height = GridSpecs.Height;

        _polygons = new Polygon2D[width, height];
        _pulses = new Pulse[width, height];
        _pressedFactor = new float[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var p = MakePolygon(new(), Vector2.One, Colors.Black);
                p.Position = new(x, y);
                _polygons[x, y] = p;
                AddChild(p);

                _pulses[x, y] = NewPulse(_rand, 1);
            }
    }

    private static Pulse NewPulse(Random rand, double delayScale) =>
        new(delayScale * MaxDelay * rand.NextDouble() + 1, MaxStretch * rand.NextDouble() + 1);

    const double MaxDelay = 60;
    const double MaxStretch = 30;

    private sealed class Pulse(double Delay, double Stretch)
    {
        public double Process(double delta, float pressed)
        {
            if (Delay > 0)
            {
                Delay = double.Max(0, Delay - delta * (1 + MaxDelay * pressed));
                return 0;
            }
            else
            {
                Delay -= delta * (1 + 0.2 * Stretch * pressed);
            }

            var halfStretch = Stretch / 2;
            var rampUp = Delay / -halfStretch;
            if (rampUp < 1) return double.Lerp(0, 0.8 / halfStretch + 0.2, rampUp);
            else return double.Lerp(0, 0.8 / halfStretch + 0.2, 2 - rampUp);
        }
    }

    public override void _Process(double delta)
    {
        for (int x = 0; x < GridSpecs.Width; x++)
            for (int y = 0; y < GridSpecs.Height; y++)
                if (IsPressed(x, y))
                {
                    void press(int xi, int yi, float factor)
                    {
                        if (xi < 0 || xi >= _pressedFactor.GetLength(0) || yi < 0 || yi >= _pressedFactor.GetLength(1))
                            return;

                        var pDelta = (float)delta * factor;
                        if (_pressedFactor[xi, yi] + pDelta < factor)
                            _pressedFactor[xi, yi] += pDelta;
                    }
                    press(x, y, 1);
                    press(x - 1, y, 0.4f);
                    press(x, y - 1, 0.4f);
                    press(x + 1, y, 0.4f);
                    press(x, y + 1, 0.4f);
                }
                else
                {
                    _pressedFactor[x, y] *= 1 - (float)delta * 0.2f;
                }

        for (int x = 0; x < GridSpecs.Width; x++)
            for (int y = 0; y < GridSpecs.Height; y++)
            {
                var isPressed = IsPressed(x, y);
                var pressedFactor = _pressedFactor[x, y];
                var pulseValue = (float)_pulses[x, y].Process(delta, pressedFactor);
                if (pulseValue < 0)
                {
                    _pulses[x, y] = NewPulse(_rand, isPressed ? 0 : 1);
                    pulseValue = 0;
                }
                if (pulseValue < 0.3f && isPressed) pulseValue = 0.3f;
                var targetColor = Color.FromOkHsl(CalculateHue(x, y), pressedFactor, pulseValue);
                _polygons[x, y].Color = _polygons[x, y].Color.Lerp(targetColor, (float)delta * 3);
            }
    }

    float CalculateHue(float x, float y)
    {
        x /= 10;
        y /= 10;
        var z = Time.GetTicksMsec() / 10000f;
        var a = OpenSimplex2S.Noise3_ImproveXY(_simplexSeedA, x, y, z);
        var b = OpenSimplex2S.Noise3_ImproveXY(_simplexSeedB, x, y, z);
        return ((Mathf.Atan2(b, a) + 1.25f * Mathf.Pi) / Mathf.Pi) % 1;
    }
}
