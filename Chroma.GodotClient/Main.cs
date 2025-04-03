using Godot;
using Humanizer;
using Humanizer.Localisation;
using MysticClue.Chroma.GodotClient.Audio;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.Games;
using MysticClue.Chroma.GodotClient.Games.Domination;
using MysticClue.Chroma.GodotClient.UI;
using MysticClue.Chroma.GodotClient.Games.ColorRun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MysticClue.Chroma.GodotClient;

/// <summary>
/// Top level entry point for the client.
///
/// The main Godot window is shown on the outside screen of each room.
///
/// This class manages which game implementation to instantiate based on user
/// selection. It also manages an inside screen SubViewport which is populated
/// from a Node provided by the game implementation.
///
/// It also, optionally, displays the hardware output in a debug window.
/// </summary>
public partial class Main : Control
{
	InsideScreen? _insideScreen;
	DebugSettingsUI? _debugSettingsUI;
	GridOutput? _gridOutput;

	const int SessionTimeSeconds = 25 * 60;
	Button? _resetSession;
	int _sessionTimeRemaining;
	Label? _sessionTimeLabel;
	Label? _gameNameLabel;
	Button? _backButton;
	PlayerSelect? _playerSelect;
	GameSelect? _gameSelect;
	Random? _seedProducer;
	Container? _feedbackSurvey;
	GridGame? _gridGame;
	ulong _gameStartTicks;

	public override void _Ready()
	{
		// Godot always logs to godot.log. If it exists, it renames the existing one using the
		// current timestamp. So the timestamp on the file does not match the contents.
		// See https://github.com/godotengine/godot/issues/44816
		var datetime = Time.GetDatetimeStringFromSystem();
		var projectVersion = ProjectSettings.GetSettingWithOverride("application/config/version");
		var assemblyVersion = Assembly.GetExecutingAssembly()
			.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
			.FirstOrDefault()?
			.InformationalVersion;
		GD.Print($"{datetime} Game Loaded - version {projectVersion} {assemblyVersion}");

		Assert.ErrorReporter = new GodotErrorReporter();

		var outsideScreenWindow = GetWindow();
		var insideScreenWindow = GetNode<Window>("InsideScreenWindow");
		_insideScreen = GetNode<InsideScreen>("%InsideScreen");
		GridGame.InsideScreen = _insideScreen;

		int screenCount = DisplayServer.GetScreenCount();
		int primaryScreen = DisplayServer.GetPrimaryScreen();
		int otherScreen = screenCount == 1 ? primaryScreen : primaryScreen == 0 ? 1 : 0;
		GD.Print($"Updating windows: count {screenCount} primary {primaryScreen} other {otherScreen}");

		void UpdateInsideScreenPosition() {
			if (!_debugSettingsUI.DebugSettings.SeparateInsideScreen) return;

			if (_debugSettingsUI.DebugSettings.FullScreen)
				insideScreenWindow.CurrentScreen = otherScreen;
			else
				insideScreenWindow.Position = outsideScreenWindow.Position + new Vector2I(outsideScreenWindow.Size.X + 1, 0);
		}

		_debugSettingsUI = GetNode<DebugSettingsUI>("%DebugSettingsUI");
		_debugSettingsUI.AddChild(new Label()
		{
			Name = "Version",
			Text = projectVersion.ToString(),
			HorizontalAlignment = HorizontalAlignment.Center
		});
		_debugSettingsUI.ShowHardwareViewToggled += (on) =>
		{
			var hardwareView = GetNode<Window>("HardwareView");
			if (on)
			{
				hardwareView.Visible = true;
				hardwareView.Position = outsideScreenWindow.Position + new Vector2I(-hardwareView.Size.X - 1, 0);
			}
			else
			{
				hardwareView.Visible = false;
			}
		};
		_debugSettingsUI.ShowFullResolutionToggled += GetNode<HardwareView>("HardwareView").SetShowFullResolution;
		_debugSettingsUI.SkipIntroToggled += (on) => { if (_gridGame != null) _gridGame.SkipIntro = on; };
		_debugSettingsUI.SeparateInsideScreenToggled += (separate) =>
		{
			if (separate)
			{
				insideScreenWindow.Show();
				UpdateInsideScreenPosition();
				_insideScreen.Reparent(insideScreenWindow, keepGlobalTransform: false);
			}
			else
			{
				insideScreenWindow.Hide();
				_insideScreen.Reparent(GetNode("%MainArea"), keepGlobalTransform: false);
			}
		};
		_debugSettingsUI.FullScreenToggled += (on) =>
		{
			if (on)
			{
				outsideScreenWindow.Mode = Window.ModeEnum.Fullscreen;
				insideScreenWindow.Mode = Window.ModeEnum.Fullscreen;
			} else
			{
				outsideScreenWindow.Mode = Window.ModeEnum.Windowed;
				insideScreenWindow.Mode = Window.ModeEnum.Windowed;
				outsideScreenWindow.Size = new(640, 360);
				insideScreenWindow.Size = new(640, 360);
				UpdateInsideScreenPosition();
			}
		};

		var config = GetNode<Config>("/root/Config");
		if (config == null || config.Hardware == null || config.Hardware.Grid == null)
		{
			throw new InvalidOperationException("Could not load config.");
		}
		if (config.LocalSettings.DebugSettings.HasValue)
		{
			_debugSettingsUI.DebugSettings = config.LocalSettings.DebugSettings.Value;
		}

		var gridSpecs = new ResolvedGridSpecs(config.Hardware.Grid);
		var hardwareView = GetNode<HardwareView>("HardwareView");
		_gridOutput = GetNode<GridOutput>("%GridOutput");
		_gridOutput.SetGrid(gridSpecs, config.Hardware.ServerEndpoint);
		hardwareView.SetGame(_gridOutput);
		_resetSession = GetNode<Button>("%ResetSession");
		_sessionTimeLabel = GetNode<Label>("%SessionTimeLabel");
		_gameNameLabel = GetNode<Label>("%GameNameLabel");
		_backButton = GetNode<Button>("%BackButton");
		_playerSelect = GetNode<PlayerSelect>("%PlayerSelect");
		_gameSelect = GetNode<GameSelect>("%GameSelect");
		_gameSelect.AllGameVariants = GetGameVariants(PlayerSelect.MaxSupportedPlayers, gridSpecs).ToList();
		_seedProducer = new Random();
		_feedbackSurvey = GetNode<Container>("%FeedbackSurvey");
		_gameSelect.AllGameVariants.Add(new GameSelection("ColorRun", new ColorRun()));


		var sessionTimer = new Timer() { Autostart = true, WaitTime = 1 };
		AddChild(sessionTimer);
		sessionTimer.Timeout += () =>
		{
			_sessionTimeRemaining = int.Max(0, _sessionTimeRemaining - 1);
			UpdateSessionTime();
		};

		_resetSession.Pressed += ResetSession;
		_playerSelect.PlayerCountSelected += PlayerCountSelected;
		_gameSelect.StartGame += StartGame;

		GetNode<Button>("%SadFace").Pressed += () => { CompleteSurvey("Sad"); };
		GetNode<Button>("%NeutralFace").Pressed += () => { CompleteSurvey("Neutral"); };
		GetNode<Button>("%HappyFace").Pressed += () => { CompleteSurvey("Happy"); };

		_backButton.Pressed += () =>
		{
			if (_insideScreen.Visible)
			{
				if (_gridGame is GridLightGame gridLightGame)
				{
					gridLightGame.On = false;
					gridLightGame.LightOff += () =>
					{
						ExitGameplay();
						ClearGame();
						EnterPlayerSelect();
					};
				}
				else
				{
					ExitGameplay();
					EnterSurvey();
				}
			}
			else if (_feedbackSurvey.Visible)
			{
				// Close survey screen and go back to game.
				ExitSurvey();
				EnterGameplay();
			}
			else if (_gameSelect.Visible)
			{
				ExitGameSelect();
				EnterPlayerSelect();
			}
		};

		_resetSession.Press();

		GetNode<Button>("%LightButton").Pressed += () =>
		{
			if (_gridGame is GridLightGame gridLightGame) return;

			ExitPlayerSelect();
			ClearGame();
			var gridGame = new GridLightGame(gridSpecs);
			gridGame.Name = "GridLightGame";
			AttachGridGame(gridGame);
		};

		_debugSettingsUI.AddButton("Input", shrink: true).Pressed += () =>
		{
			ClearGame();
			var gridGame = new GridInputGame(gridSpecs);
			gridGame.Name = "GridInputGame";
			AttachGridGame(gridGame);
		};
		_debugSettingsUI.AddButton("Spectrometer", shrink: true).Pressed += () =>
		{
			ClearGame();
			var gridGame = new GridSpectrometerGame(gridSpecs);
			gridGame.Name = "Spectrometer";
			AttachGridGame(gridGame);
		};
	}

	void ResetSession()
	{
		GD.Print("Session reset");
		ClearGame();
		ExitGameplay();
		ExitSurvey();
		ExitGameSelect();
		EnterPlayerSelect();
		_sessionTimeRemaining = SessionTimeSeconds;
		_sessionTimeLabel?.RemoveThemeColorOverride("font_color");
		UpdateSessionTime();
	}

	void UpdateSessionTime()
	{
		var remainingMinutes = double.Ceiling(_sessionTimeRemaining / 60.0);
		var timeRemainingText = TimeSpan.FromMinutes(remainingMinutes).Humanize(minUnit: TimeUnit.Minute);
		if (_sessionTimeLabel != null)
		{
			_sessionTimeLabel.Text = $"Session time: {timeRemainingText}";
			if (_sessionTimeRemaining == 0)
			{
				if (_sessionTimeLabel.HasThemeColorOverride("font_color"))
					_sessionTimeLabel.RemoveThemeColorOverride("font_color");
				else
					_sessionTimeLabel.AddThemeColorOverride("font_color", Colors.Red);
			}
		}
	}

	void EnterPlayerSelect()
	{
		_backButton?.Hide();
		_playerSelect?.Show();
		_resetSession?.Show();
	}

	void ExitPlayerSelect()
	{
		_playerSelect?.Hide();
		_resetSession?.Hide();
	}

	void PlayerCountSelected(int playerCount)
	{
		ExitPlayerSelect();
		_gameSelect?.SetPlayerCount(playerCount);
		EnterGameSelect();
	}

	void EnterGameSelect()
	{
		_backButton?.Show();
		_gameSelect?.Show();
	}

	void ExitGameSelect()
	{
		_backButton?.Hide();
		_gameSelect?.Hide();
	}

	void StartGame()
	{
		if (_gameSelect!.CurrentGameSelection.HasValue)
		{
			ExitGameSelect();
			ClearGame();
			var g = _gameSelect.CurrentGameSelection.Value;
			var seed = _seedProducer!.Next();
			GDPrint.WithTimestamp($"Starting {g} with seed = {seed}");
			var gridGame = g.Instantiate(seed);
			gridGame.Name = g.Name;
			_gameNameLabel!.Text = $"{g.Name} {g.Level ?? ""}";
			_gameNameLabel.Show();
			_sessionTimeLabel?.Hide();
			AttachGridGame(gridGame);
		}
	}

	void AttachGridGame(GridGame gridGame)
	{
		_gameStartTicks = Time.GetTicksMsec();
		_gridGame = gridGame;
		gridGame.SkipIntro = _debugSettingsUI!.DebugSettings.SkipIntro;
		_gridOutput?.SetGame(gridGame);
		EnterGameplay();
	}

	void EnterGameplay()
	{
		_backButton?.Show();
		_insideScreen?.Show();
	}

	void ExitGameplay()
	{
		_backButton?.Hide();
		_insideScreen?.Hide();
	}

	void EnterSurvey()
	{
		_backButton?.Show();
		_feedbackSurvey?.Show();
	}

	void ExitSurvey()
	{
		_backButton?.Hide();
		_feedbackSurvey?.Hide();
	}

	void CompleteSurvey(string result)
	{
		if (_gridGame != null)
		{
			GDPrint.WithTimestamp($"Game ended after {(Time.GetTicksMsec() - _gameStartTicks) / 1000}s in state {_gridGame.StateString} survey feedback {result}");
		}
		ExitSurvey();
		ClearGame();
		EnterGameSelect();
	}

	void ClearGame()
	{
		_gridGame = null;
		_gridOutput?.SetGame(null);
		_gameNameLabel!.Text = "";
		_gameNameLabel.Hide();
		_sessionTimeLabel?.Show();
		_insideScreen?.ClearGame();
		GetNode<AudioManager>("/root/AudioManager")?.StopAll(2);
	}

	public override void _Input(InputEvent @event)
	{
		// We have to handle inputs here because buttons only receive input when visible.

		if (@event.IsActionPressed("debugMenu"))
			GetNode<PanelContainer>("%DebugPanel").Visible ^= true;

		if (@event.IsActionPressed("resetSession"))
			GetNode<Button>("%ResetSession").Press();
	}

	private static GameSelection.GetGameVariants[] _gameVariantProviders = [
		GridBattleGame.GetGameVariants,
		GridDominationCoopGameVariants.GetGameVariants,
		GridDominationPvpGameVariants.GetGameVariants,
		GridCollectGame.GetGameVariants,
		GridStarfieldGame.GetGameVariants,
		GridRandomWalkGame.GetGameVariants,
		GridMemoryGame.GetGameVariants,
		GridRhythmGame.GetGameVariants,
		ColorRun.GetGameVariants,
#if DEBUG
		GridTemplateGame.GetGameVariants,
#endif
		GridChallengeGame.GetGameVariants,
	];

	public static IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs)
	{
		return _gameVariantProviders.SelectMany(p => p(maxSupportedPlayers, gridSpecs));
	}
}
