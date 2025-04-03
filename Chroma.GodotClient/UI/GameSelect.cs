using Godot;
using MysticClue.Chroma.GodotClient.GameLogic;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.Games;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MysticClue.Chroma.GodotClient.UI;

/// <summary>
/// UI that manages game selection.
/// 
/// The list of AllGameVariants and the CurrentPlayerCount must be provided.
/// </summary>
public partial class GameSelect : VBoxContainer
{
    [Export]
    StyleBox? ButtonNormalStyle;
    [Export]
    StyleBox? LevelNormalStyle;
    [Export]
    StyleBox? LevelPressedStyle;

    public int CurrentPlayerCount { get; private set; }
    public void SetPlayerCount(int playerCount)
    {
        CurrentPlayerCount = playerCount;
        UpdateGameList();
    }

    IReadOnlyList<GameSelection> _allGameVariants = [];
    public IReadOnlyList<GameSelection> AllGameVariants
    {
        get => _allGameVariants;
        set { _allGameVariants = value; UpdateAllVariants(); }
    }

    public GameSelection? CurrentGameSelection
    {
        get
        {
            var gotGameSet = _gameSetsByNameLevel.TryGetValue(_currentGameSet, out var gameSet);
            if (!Assert.That(gotGameSet)) return null;
            var gotSelection = gameSet!.TryGetValue(_currentDifficulty, out var selection);
            if (!Assert.That(gotSelection)) return null;
            return selection;
        }
    }

    [Signal]
    public delegate void StartGameEventHandler();

    // GameSelections grouped by name and level.
    Dictionary<(string, string?), Dictionary<GameSelection.GameDifficulty, GameSelection>> _gameSetsByNameLevel = [];
    // Buttons for each gameSet.
    Dictionary<(string, string?), Button> _gameSetButtons = [];
    Dictionary<GameSelection.GameType, Button> _gameTypeSelect = [];
    Dictionary<string, PanelContainer> _gameLevelContainers = [];
    Label? _howToPlayText;
    TextureRect? _howToPlayImage;
    ButtonGroup _gameButtonGroup = new();
    Dictionary<GameSelection.GameDifficulty, Button> _difficultySelect = [];
    GameSelection.GameType _currentGameType;
    (string, string?) _currentGameSet;
    GameSelection.GameDifficulty _currentDifficulty;

    public override void _Ready()
    {
        base._Ready();

        _gameTypeSelect = new()
        {
            [GameSelection.GameType.Cooperative] = GetNode<Button>("%CooperativeGameType"),
            [GameSelection.GameType.Competitive] = GetNode<Button>("%CompetitiveGameType"),
            [GameSelection.GameType.Zen] = GetNode<Button>("%ZenGameType"),
        };
        _howToPlayText = GetNode<Label>("%HowToPlayText");
        _howToPlayImage = GetNode<TextureRect>("%HowToPlayImage");
        _difficultySelect = new()
        {
            [GameSelection.GameDifficulty.Newbie] = GetNode<Button>("%NewbieDifficulty"),
            [GameSelection.GameDifficulty.Regular] = GetNode<Button>("%RegularDifficulty"),
            [GameSelection.GameDifficulty.Elite] = GetNode<Button>("%EliteDifficulty"),
        };
        foreach (var (d, db) in _difficultySelect) db.Pressed += () =>
        {
            if (_gameSetsByNameLevel.TryGetValue(_currentGameSet, out var gameSet))
            {
                if (gameSet.ContainsKey(d))
                {
                    _currentDifficulty = d;
                    return;
                }
                else
                {
                    Assert.Report(new InvalidOperationException("Invalid difficulty selected"));
                }
            }
            else
            {
                Assert.Report(new InvalidOperationException("Invalid gameSet selected."));
            }
            // Go back to previous selection.
            _difficultySelect[_currentDifficulty].Press();
        };

        foreach (var (gt, b) in _gameTypeSelect) b.Pressed += () =>
        {
            _currentGameType = gt;
            UpdateGameList();
        };
        UpdateAllVariants();
        _gameTypeSelect.Values.First().Press();
        GetNode<Button>("%StartGame").Pressed += () =>
        {
            if (CurrentGameSelection != null) EmitSignal(SignalName.StartGame);
        };
    }

    private void UpdateAllVariants()
    {
        if (!IsNodeReady()) return;

        var gameList = GetNode<Container>("%GameList");
        if (Assert.ReportNull(gameList)) return;

        foreach (var c in gameList.GetChildrenByType<CanvasItem>())
        {
            // Have Godot randomly re-assign these names so that the new ones we add keep the
            // names we ask for.
            c.Name = "QueuedForDeletion";
            // Hide so that checking visibility will not include these old ones.
            c.Hide();
            c.QueueFree();
        };

        // Add a button for each (name, level).
        // Put levels of the same game together.
        _gameLevelContainers = [];
        foreach (var g in _allGameVariants)
        {
            var gameSet = (g.Name, g.Level);
            if (_gameSetButtons.ContainsKey(gameSet)) continue;

            Button b;
            if (g.Level != null)
            {
                VBoxContainer vBox;
                if (_gameLevelContainers.GetOrNew(g.Name, out var panel))
                {
                    panel.Name = g.Name;
                    panel.SizeFlagsHorizontal = SizeFlags.Fill;
                    panel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                    panel.AddThemeStyleboxOverride("panel", ButtonNormalStyle);
                    vBox = new VBoxContainer();
                    vBox.SizeFlagsHorizontal = SizeFlags.Fill;
                    vBox.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                    vBox.AddChild(new Label() { Text = g.Name, HorizontalAlignment = HorizontalAlignment.Center });
                    panel.AddChild(vBox);
                    gameList.AddChild(panel);
                }
                else
                {
                    vBox = panel.GetChild<VBoxContainer>(0);
                }

                b = vBox.AddButton(g.Level);
                b.AddThemeStyleboxOverride("normal", LevelNormalStyle);
                b.AddThemeStyleboxOverride("hover", LevelNormalStyle);
                b.AddThemeStyleboxOverride("pressed", LevelPressedStyle);
                b.AddThemeStyleboxOverride("focus", LevelPressedStyle);
            }
            else
            {
                b = gameList.AddButton(g.Name);
            }
            b.ActionMode = BaseButton.ActionModeEnum.Press;
            b.MouseFilter = MouseFilterEnum.Pass;
            b.ButtonGroup = _gameButtonGroup;
            b.ToggleMode = true;
            _gameSetButtons[gameSet] = b;
            b.Pressed += () =>
            {
                _currentGameSet = gameSet;

                if (!_gameSetsByNameLevel.TryGetValue(gameSet, out var gs)) return;

                // Update available difficulties.
                foreach (var (d, db) in _difficultySelect)
                    db.Visible = gs.ContainsKey(d);

                // Select a new difficulty if the old one is no longer available.
                // Otherwise, ensure the current difficulty is selected (e.g. at startup).
                if (!_difficultySelect[_currentDifficulty].Visible)
                    _difficultySelect[gs.Keys.FirstOrDefault()].Press();
                else
                    _difficultySelect[_currentDifficulty].Press();

                // Update HowToPlay.
                if (CurrentGameSelection != null)
                {
                    if (_howToPlayText != null)
                        _howToPlayText.Text = gs[_currentDifficulty].HowToPlayText;
                    if (_howToPlayImage != null)
                        _howToPlayImage.Texture = gs[_currentDifficulty].HowToPlayImage;
                }

                // Work around scroll not working by scrolling so the selected button is in the middle.
                var scroll = gameList.GetParent<ScrollContainer>();
                var scrollCenter = scroll.GetGlobalRect().GetCenter().Y;
                var buttonCenter = b.GetGlobalRect().GetCenter().Y;
                var targetScroll = scroll.ScrollVertical + (int)(buttonCenter - scrollCenter);
                var setScroll = Callable.From((int v) => scroll.ScrollVertical = v);
                scroll.CreateTween()
                    .TweenMethod(setScroll, scroll.ScrollVertical, targetScroll, 0.1f);
            };
        }

        UpdateGameList();
    }

    private void UpdateGameList()
    {
        if (Assert.ReportNull(_gameTypeSelect)) return;

        // Fill _gameSetsByNameLevel with only games of the selected type that support CurrentPlayerCount.
        _gameSetsByNameLevel.Clear();
        foreach (var g in _allGameVariants)
        {
            if (g.Type != _currentGameType || g.PlayerCount != CurrentPlayerCount)
                continue;

            _gameSetsByNameLevel.GetOrNew((g.Name, g.Level), out var gameSet);
            gameSet[g.Difficulty] = g;
        }

        // Only show buttons with a corresponding gameSet.
        foreach (var (gameSet, b) in _gameSetButtons)
            b.Visible = _gameSetsByNameLevel.ContainsKey(gameSet);

        // Hide games with no visible levels.
        foreach (var c in _gameLevelContainers.Values)
            c.Visible = c.GetChildrenByType<Button>().Any(b => b.Visible);

        var startGameButton = GetNode<Button>("%StartGame");
        startGameButton.Visible = true;

        if (_howToPlayImage != null) _howToPlayImage.Texture = null;

        // Select the first button by default. (To avoid a state where nothing is selected).
        var currentButton = _gameSetButtons.Values.FirstOrDefault(b => b.ButtonPressed);
        if (currentButton == null || !currentButton.Visible)
            currentButton = _gameSetButtons.Values.FirstOrDefault(b => b.Visible);

        if (currentButton == null)
        {
            // No games in this category.
            foreach (var db in _difficultySelect.Values)
                db.Visible = false;

            GetNode<Container>("%DifficultySelect").Visible = false;

            _currentGameSet = default;
            startGameButton.Visible = false;
        }
        else
        {
            GetNode<Container>("%DifficultySelect").Visible = true;
            currentButton.Press();
        }
    }
}
