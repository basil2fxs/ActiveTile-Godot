using Godot;
using MysticClue.Chroma.GodotClient.Audio;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MysticClue.Chroma.GodotClient.Games;

/// <summary>
/// A template for creating new games.
/// We try to keep this updated with the latest way to do things.
/// </summary>
public partial class GridTemplateGame : GridGame
{
    public static IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs)
    {
        for (int playerCount = 1; playerCount <= maxSupportedPlayers; playerCount++)
            yield return new(
                playerCount,
                GameSelection.GameType.Cooperative,
                GameSelection.GameDifficulty.Regular,
                "Template",
                null,
                "<game explanation here>",
                GD.Load<Texture2D>("res://HowToPlay/placeholder.png"),
                randomSeed => new GridTemplateGame(gridSpecs, randomSeed));
    }

    // These are container nodes that hold different types of game objects.
    // It makse looking up the type of an object easier because you can just check its parent.
    Node2D _gameObjects1 = new() { Name = "First type of object" };
    Node2D _gameObjects2 = new() { Name = "Second type of object" };

    // These are extra container nodes for putting animations and other effects into.
    Node2D _bottomLayer = new() { Name = "BottomLayer" };
    Node2D _topLayer = new() { Name = "TopLayer" };

    // Any game parameters and implementation details.
    ulong _startTime;
    List<Polygon2D> _polygons = [];

    private enum States
    {
        // Initially everything is dark waiting for players to step into room.
        // We trigger the flourish upon the first step, then go to Countdown once the flourish finishes.
        Flourish,
        // Setup animation and countdown to game start.
        Countdown,
        // Main play loop.
        Play,
        // End of game animations and freeze.
        Win,
        Lose,
    };
    private States _state;
    public override string StateString => _state.ToString();

    protected override IReadOnlyList<Sounds> UsedSounds => [
        Sounds.Music_Challenge_8Bit,
        Sounds.Flourish_90sGameUi4,
        Sounds.Countdown_CountdownSoundEffect8Bit,
        Sounds.GameStart_8BitBlastE,
        Sounds.Positive_90sGameUi2,
        Sounds.Negative_8BitGame1,
        Sounds.GameWin_PowerUpSparkle1,
        Sounds.GameLose_90sGameUi15,
    ];

    public GridTemplateGame(ResolvedGridSpecs grid, int randomSeed) : base(grid)
    {
        GDPrint.LogState(randomSeed);

        InsideScreen!.Message.Show();

        Vector2I gridSize = new(GridSpecs.Width, GridSpecs.Height);

        // Randomly (but deterministically) add some tiles.
        var rand = new Random(randomSeed);
        for (int i = 0; i < 10; i++)
        {
            Vector2I size = Vector2I.One;
            Vector2I position = new(rand.Next(gridSize.X - size.X), rand.Next(gridSize.Y - size.Y));
            uint color = (uint)rand.Next();
            color <<= 1;
            color |= 255;
            var p = MakePolygon(new(), size, new(color));
            p.Position = position;
            _polygons.Add(p);
            _gameObjects1.AddChild(p);
        }

        // Add in render order.
        AddChild(_bottomLayer);
        AddChild(_gameObjects1);
        AddChild(_gameObjects2);
        AddChild(Flourish);
        AddChild(_topLayer);

        EnterFlourish();
    }

    private void EnterFlourish()
    {
        _state = States.Flourish;
        _gameObjects1.Hide();
        _gameObjects2.Hide();
        EnterFlourishState(EnterCountdown);
    }

    private void EnterCountdown()
    {
        _state = States.Countdown;
        InsideScreen!.Message.Text = "Get Ready";

        // Scale the time between countdown steps.
        float countdownIntervalSeconds = SkipIntro ? 0.1f : 1f;

        // Flash in some polygons on each count.
        const int CountdownIntervals = 5;
        for (int s = 0; s <= CountdownIntervals - 1; ++s)
        {
            for (int i = s; i < _gameObjects1.GetChildCount(); i += CountdownIntervals)
            {
                EaseInFlashWhite(_gameObjects1.GetChild<Polygon2D>(i), countdownIntervalSeconds * (s + 1));
            }
        }
        _gameObjects1.Show();

        // The countdown goes beep-beep-beep-booop.
        if (!SkipIntro)
        {
            this.AddTimer(2 * countdownIntervalSeconds).Timeout += () =>
            {
                AudioManager?.Play(Sounds.Countdown_CountdownSoundEffect8Bit);
            };
        }

        // Start the game after countdown is done.
        this.AddTimer(countdownIntervalSeconds * (CountdownIntervals + 1)).Timeout += EnterPlay;
    }

    private void EnterPlay()
    {
        _state = States.Play;
        InsideScreen!.Message.Text = "Go!";
        _startTime = Time.GetTicksMsec();
        AudioManager?.PlayMusic(Sounds.Music_Challenge_8Bit);
    }
    private void ProcessPlay(double delta)
    {
        // Check for presses since last time.
        foreach (var point in TakePressed())
        {
            foreach (var p in _polygons)
            {
                if (p.Color.A == 1 && new Vector2I((int)p.Position.X, (int)p.Position.Y) == point)
                {
                    FlashSquare(_topLayer, point, p.Color, Colors.White, flashTwice: true);
                    p.Color = p.Color with { A = 0.2f };
                }
            }
        }

        if (_polygons.All(p => p.Color.A == 0.2f)) EnterWin();

        if (Time.GetTicksMsec() - _startTime > 180000) EnterLose();
    }

    private void EnterWin()
    {
        _state = States.Win;
        InsideScreen!.Message.Text = "You Win";
        AudioManager?.Play(Sounds.GameWin_PowerUpSparkle1);
        AudioManager?.StopMusic();
    }

    private void EnterLose()
    {
        _state = States.Lose;
        InsideScreen!.Message.Text = "Game Over";
        AudioManager?.Play(Sounds.GameLose_90sGameUi15);
        AudioManager?.StopMusic();
    }

    public override void _Process(double delta)
    {
        if (_state == States.Flourish) ProcessFlourish();
        else if (_state == States.Play) ProcessPlay(delta);
    }
}
