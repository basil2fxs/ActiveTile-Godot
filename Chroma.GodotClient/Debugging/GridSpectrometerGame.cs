using Godot;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.GameLogic.Grid.Serial;
using MysticClue.Chroma.GodotClient.Games;
using System;
using System.Linq;

namespace MysticClue.Chroma.GodotClient;

/// <summary>
/// Debugging game for displaying gradually stepped colors so we can compare the tile with the screen.
///
/// Also gets readings from a serial-attached spectrometer.
/// </summary>
public partial class GridSpectrometerGame : GridGame
{
    Container _spectrumContainer = new();
    Polygon2D _spectrum = new();
    int _currentAmplitude = 25;
    Color _currentColor = Colors.Red;
    int[] _latestSpectrum = [];

    Polygon2D _background;
    Polygon2D[] _gradient;

    SerialPort? _serialPort;

    public GridSpectrometerGame(ResolvedGridSpecs grid) : base(grid)
    {
        _background = MakePolygon(new(0, 0), new(grid.Width, grid.Height), Colors.Black);
        AddChild(_background);
        _gradient = new Polygon2D[grid.Height];
        for (int i = 0; i < grid.Height; ++i)
        {
            var g = MakePolygon(new(0, 0), new(grid.Width / 2, 1), Colors.Black);
            g.Position = new(grid.Width / 2, i);
            AddChild(g);
            _gradient[i] = g;
        }
        UpdateBackground();

        var buttonContainer = new HFlowContainer();
        InsideScreen!.PerGameContainer.AddChild(buttonContainer);
        InsideScreen!.PerGameContainer.AddChild(_spectrumContainer);
        _spectrumContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _spectrumContainer.SizeFlagsVertical |= Control.SizeFlags.Expand;
        _spectrumContainer.AddChild(_spectrum);
        _spectrum.VertexColors = [
            Colors.Violet, Colors.Indigo, Colors.Blue, Colors.Cyan, Colors.Green, Colors.Yellow, Colors.Orange, Colors.Red,
            Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green, Colors.Cyan, Colors.Blue, Colors.Indigo, Colors.Violet,
        ];

        foreach (var port in SerialPort.GetPortNames())
        {
            var b = new Button() { Name = port, Text = port };
            buttonContainer.AddChild(b);
            b.Pressed += () =>
            {
                _serialPort = new SerialPort(port);
            };
        }
        {
            var b = new Button() { Name = "RGB", Text = "RGB" };
            buttonContainer.AddChild(b);
            b.Pressed += () =>
            {
                if (_currentColor == Colors.Red) { _currentColor = Colors.Green; }
                else if (_currentColor == Colors.Green) { _currentColor = Colors.Blue; }
                else { _currentColor = Colors.Red; }
                _currentAmplitude = 25;
                UpdateBackground();
            };
        }
        {
            var b = new Button() { Name = "CMYW", Text = "CMYW" };
            buttonContainer.AddChild(b);
            b.Pressed += () =>
            {
                if (_currentColor == Colors.Cyan) { _currentColor = Colors.Magenta; }
                else if (_currentColor == Colors.Magenta) { _currentColor = Colors.Yellow; }
                else if (_currentColor == Colors.Yellow) { _currentColor = Colors.White; }
                else { _currentColor = Colors.Cyan; }
                _currentAmplitude = 25;
                UpdateBackground();
            };
        }
        {
            var b = new Button() { Name = "AmplitudeSwitch", Text = "+>" };
            buttonContainer.AddChild(b);
            b.Pressed += () =>
            {
                if (_currentAmplitude < 250) { _currentAmplitude += 25; }
                UpdateBackground();
            };
        }
        {
            var b = new Button() { Name = "SnapshotSpectrum", Text = "[]" };
            buttonContainer.AddChild(b);
            b.Pressed += () =>
            {
                GD.Print($"{_background.Color.ToRgba32():X} {string.Join(' ', _latestSpectrum)}");
            };
        }
    }

    private void UpdateBackground()
    {
        _background.Color = _currentColor * new Color(_currentAmplitude / 255f, _currentAmplitude / 255f, _currentAmplitude / 255f, 1);
        float gradients = _gradient.Length - 1;
        for (int i = 0; i < _gradient.Length; ++i)
        {
            var g = i / gradients;
            _gradient[i].Color = _currentColor * new Color(g, g, g);
        }
    }

    public override void _Process(double delta)
    {
        if (_serialPort != null)
        {
            try
            {
                var line = _serialPort.Actual.ReadLine();
                var spectrum = line.Split(' ').Select(int.Parse).ToArray();
                UpdateSpectrum(spectrum);
            }
            catch (TimeoutException) { }
            catch (FormatException) { }
        }

        foreach (var (x, y) in TakePressed())
        {
            if (y != 0) continue;

            if (x == 0) _currentColor = Colors.Red;
            if (x == 1) _currentColor = Colors.Green;
            if (x == 2) _currentColor = Colors.Blue;
            if (x == 3) _currentColor = Colors.Cyan;
            if (x == 4) _currentColor = Colors.Magenta;
            if (x == 5) _currentColor = Colors.Yellow;
            if (x == 6) _currentColor = Colors.White;

            UpdateBackground();
        }
    }

    private void UpdateSpectrum(int[] spectrum)
    {
        _latestSpectrum = spectrum;
        var rect = _spectrumContainer.GetRect();
        _spectrum.Polygon = [
            new(0 * rect.Size.X / 7, rect.Size.Y - rect.Size.Y * spectrum[0] / ushort.MaxValue),
            new(1 * rect.Size.X / 7, rect.Size.Y - rect.Size.Y * spectrum[1] / ushort.MaxValue),
            new(2 * rect.Size.X / 7, rect.Size.Y - rect.Size.Y * spectrum[2] / ushort.MaxValue),
            new(3 * rect.Size.X / 7, rect.Size.Y - rect.Size.Y * spectrum[3] / ushort.MaxValue),
            new(4 * rect.Size.X / 7, rect.Size.Y - rect.Size.Y * spectrum[4] / ushort.MaxValue),
            new(5 * rect.Size.X / 7, rect.Size.Y - rect.Size.Y * spectrum[5] / ushort.MaxValue),
            new(6 * rect.Size.X / 7, rect.Size.Y - rect.Size.Y * spectrum[6] / ushort.MaxValue),
            new(7 * rect.Size.X / 7, rect.Size.Y - rect.Size.Y * spectrum[7] / ushort.MaxValue),
            new(7 * rect.Size.X / 7, rect.Size.Y),
            new(6 * rect.Size.X / 7, rect.Size.Y),
            new(5 * rect.Size.X / 7, rect.Size.Y),
            new(4 * rect.Size.X / 7, rect.Size.Y),
            new(3 * rect.Size.X / 7, rect.Size.Y),
            new(2 * rect.Size.X / 7, rect.Size.Y),
            new(1 * rect.Size.X / 7, rect.Size.Y),
            new(0 * rect.Size.X / 7, rect.Size.Y),
        ];
    }
}
