using Godot;
using System;
using System.Collections.Generic;

namespace MysticClue.Chroma.GodotClient.Games
{
	public partial class ColorRun : Node
	{
		private Label _colorLabel = default!;
		private Random _random = new Random();
		private List<Color> _colors = new List<Color>() {
			new Color(1, 0, 0),  // Red
			new Color(0, 1, 0),  // Green
			new Color(0, 0, 1),  // Blue
			new Color(1, 1, 0),  // Yellow
			new Color(0, 1, 1),  // Cyan
			new Color(1, 0, 1)   // Magenta
		};
		
		private int _requiredTiles = 0;
		private float _timePerRound = 3f; // Start with 3 seconds
		private int _currentRound = 0;
		private int _playerCount = 1; // Update this based on actual player count

		public override void _Ready()
		{
			_colorLabel = new Label();
			AddChild(_colorLabel);
			StartNewRound();
		}

		public void StartNewRound()
		{
			_currentRound++;
			_requiredTiles = 4 * _playerCount; // 4 tiles per player
			_timePerRound = Math.Max(1f, 3f - (_currentRound * 0.2f)); // Decreases time each round
			
			// Get a random color
			int randomIndex = _random.Next(_colors.Count);
			Color randomColor = _colors[randomIndex];

			// Display the color and start the timer
			ShowColorPrompt(randomColor);

			Timer timer = new Timer();
			AddChild(timer);
			timer.WaitTime = _timePerRound;
			timer.OneShot = true;
			timer.Connect("timeout", new Callable(this, nameof(OnRoundTimeout)));
			timer.Start();
		}

		private void ShowColorPrompt(Color color)
		{
			_colorLabel.Text = $"Go to: {ColorToString(color)}";
			_colorLabel.Modulate = color;
		}

		private string ColorToString(Color color)
		{
			if (color == new Color(1, 0, 0)) return "Red";
			if (color == new Color(0, 1, 0)) return "Green";
			if (color == new Color(0, 0, 1)) return "Blue";
			if (color == new Color(1, 1, 0)) return "Yellow";
			if (color == new Color(0, 1, 1)) return "Cyan";
			if (color == new Color(1, 0, 1)) return "Magenta";
			return "Unknown";
		}

		private void OnRoundTimeout()
		{
			GD.Print("Round failed! Time is up.");
			// You can call StartNewRound again if the game continues after failure
		}

		public void PlayerReachedCorrectTile()
		{
			_requiredTiles--;
			if (_requiredTiles <= 0)
			{
				GD.Print("Round complete! Starting new round.");
				StartNewRound();
			}
		}

		public static IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs)
		{
			return new List<GameSelection> {
				new GameSelection("ColorRun", new ColorRun())
			};
		}
	}
}
