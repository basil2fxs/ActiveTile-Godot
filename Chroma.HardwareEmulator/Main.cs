using MysticClue.Chroma.GodotClient.GameLogic;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace MysticClue.Chroma.HardwareEmulator;

public partial class Main : Form, ISensorView, IFrameView
{
    private ResolvedGridSpecs? _inboundGridSpecs;
    private GridTile[,]? _gridTiles;
    private IHardwareGameInterface? _inboundInterface;
    private System.Windows.Forms.Timer _pushSensorDataTimer;
    private ResolvedGridSpecs? _outboundGridSpecs;
    private IGameHardwareInterface? _outboundInterface;
    private bool[,]? _outboundPressed;

    // Limit the rate that sensor data is sent back.
    public const int SensorFpsLimit = 60;

    public Main()
    {
        InitializeComponent();
        Console.SetOut(new ControlWriter(consoleTextBox));
        LoadDefaultSpecs();
        _pushSensorDataTimer = new();
        _pushSensorDataTimer.Tick += _pushSensorDataTimer_Tick;
        _pushSensorDataTimer.Interval = 1000 / SensorFpsLimit;
    }

    private void _pushSensorDataTimer_Tick(object? sender, EventArgs e)
    {
        if (_inboundInterface != null) _ = _inboundInterface.PutSensorData(this);

        if (_outboundInterface != null) _ = _outboundInterface.PutFrame(this);
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private async void LoadDefaultSpecs()
    {
        // For convenience when developing, we hard code loading a local hardware.json from the project root.
        try
        {
            var path = "../../../../../Chroma.HardwareEmulator/hardware.json";
            if (File.Exists(path)) await LoadSpecs(path, inboundSpecTextBox);
        }
        catch (IOException ex) { Console.WriteLine(ex); }
    }

    private static async Task LoadSpecs(string path, TextBox textBox)
    {
        var hardwareJson = await File.ReadAllTextAsync(path);
        textBox.Text = hardwareJson.ReplaceLineEndings();
    }

    private static bool InBounds<T>(int x, int y, [NotNullWhen(true)] T[,]? array) =>
        array != null && x >= 0 && x < array.GetLength(0) && y >= 0 && y < array.GetLength(1);

    private void UpdatePixel(int x, int y, byte r, byte g, byte b)
    {
        if (_gridTiles == null) return;

        // _gridTiles is built from the inbound HardwareSpec, so we expect this to be correct.
        Assert.That(InBounds(x, y, _gridTiles));

        _gridTiles[x, y].BackColor = Color.FromArgb(r, g, b);
    }

    public (byte, byte, byte) GetPixel(int x, int y)
    {
        // The outbound grid might not be the same size as inbound, so just ignore if out of bounds.
        if (!InBounds(x, y, _gridTiles)) return (0, 0, 0);

        var c = _gridTiles[x, y].BackColor;
        return (c.R, c.G, c.B);
    }

    private void UpdateSensor(int x, int y, byte sensorData)
    {
        // The outbound grid might not be the same size as inbound, so just ignore if out of bounds.
        if (!InBounds(x, y, _outboundPressed)) return;

        _outboundPressed[x, y] = sensorData == ResolvedGridSpecs.SensorValuePressed;
    }

    public byte GetTile(int x, int y)
    {
        // The outbound grid might not be the same size as inbound, so just ignore if out of bounds.
        if (InBounds(x, y, _gridTiles) && _gridTiles[x, y] != null && _gridTiles[x, y].Pressed)
        {
            return ResolvedGridSpecs.SensorValuePressed;
        }
        if (InBounds(x, y, _outboundPressed) && _outboundPressed[x, y])
        {
            return ResolvedGridSpecs.SensorValuePressed;
        }

        return ResolvedGridSpecs.SensorValueUnpressed;
    }

    private void ClearGrid()
    {
        if (_gridTiles != null)
        {
            foreach (var p in _gridTiles)
            {
                gridContainer.Controls.Remove(p);
                p.Dispose();
            }
        }
        gridContainer.Size = new();
        _gridTiles = null;
    }

    private void UpdateGrid()
    {
        if (_inboundGridSpecs == null) { return; }

        var width = _inboundGridSpecs.Width;
        var height = _inboundGridSpecs.Height;
        if (_gridTiles?.GetLength(0) != width || _gridTiles?.GetLength(1) != height)
        {
            ClearGrid();

            _gridTiles = new GridTile[width, height];
            const int CELL_SIZE = 30;
            gridContainer.Size = new(CELL_SIZE * width, CELL_SIZE * height);
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    var t = new GridTile()
                    {
                        Width = CELL_SIZE - 1,
                        Height = CELL_SIZE - 1,
                        Location = new(CELL_SIZE * x, CELL_SIZE * y),
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.Black
                    };
                    _gridTiles[x, y] = t;
                    gridContainer.Controls.Add(t);
                }
            }
        }
    }

    private static void ValidateHardwareSpec(HardwareSpec spec)
    {
        if (spec.Grid != null)
        {
            var g = spec.Grid;
            for (int i = 0; i < g.OutputChains.Count; ++i)
            {
                // If any use serial, we should be fine (it'll have to pass ResolvedGridSpecs validation).
                if (g.OutputChains[i].SerialPort != null) return;

                for (int j = 0; j < i; ++j)
                {
                    if (g.OutputChains[i].Endpoint == g.OutputChains[j].Endpoint)
                    {
                        throw new ArgumentException($"Duplicate endpoint: {g.OutputChains[i]} (emulator requires unique endpoints).");
                    }
                }
            }
        }

        if (spec.ServerEndpoint == null)
        {
            throw new ArgumentException($"ServerEndpoint must be set.");
        }
    }

    private void inboundSpecTextBox_TextChanged(object sender, EventArgs e)
    {
        try
        {
            var hardwareSpec = JsonConfig.FromJsonString<HardwareSpec>(inboundSpecTextBox.Text);
            inboundErrorIndicatorLabel.Text = "";
            if (hardwareSpec?.Grid != null)
            {
                ValidateHardwareSpec(hardwareSpec);
                _inboundGridSpecs = new(hardwareSpec.Grid);
                UpdateGrid();
                if (_inboundInterface != null)
                {
                    _pushSensorDataTimer.Stop();
                    _inboundInterface.Dispose();
                }

                if (_inboundGridSpecs.UseSerial)
                    _inboundInterface = new SerialHardwareGameInterface(UpdatePixel, new GodotClient.GameLogic.Grid.Serial.SerialPortFactory());
                else
                    _inboundInterface = new UdpHardwareGameInterface(hardwareSpec.ServerEndpoint!, UpdatePixel);
                _inboundInterface.UpdateGrid(_inboundGridSpecs);
                _pushSensorDataTimer.Start();
            }
        }
        catch (Exception ex)
        {
            inboundErrorIndicatorLabel.Text = "!";
            inboundErrorToolTip.SetToolTip(inboundErrorIndicatorLabel, ex.Message + "\n" + ex.StackTrace);
        }
    }

    private void outboundSpecTextBox_TextChanged(object sender, EventArgs e)
    {
        try
        {
            var hardwareSpec = JsonConfig.FromJsonString<HardwareSpec>(outboundSpecTextBox.Text);
            outboundErrorIndicatorLabel.Text = "";
            if (hardwareSpec?.Grid != null)
            {
                ValidateHardwareSpec(hardwareSpec);
                _outboundGridSpecs = new(hardwareSpec.Grid);
                UpdateGrid();
                if (_outboundInterface != null) _outboundInterface.Dispose();

                if (_outboundGridSpecs.UseSerial)
                    _outboundInterface = new SerialGameHardwareInterface(_outboundGridSpecs, new GodotClient.GameLogic.Grid.Serial.SerialPortFactory());
                else
                    _outboundInterface = new UdpGameHardwareInterface(_outboundGridSpecs, hardwareSpec.ServerEndpoint!);

                _outboundPressed = new bool[_outboundGridSpecs.Width, _outboundGridSpecs.Height];
                _outboundInterface.UpdateSensorCallback = UpdateSensor;
            }
        }
        catch (Exception ex)
        {
            outboundErrorIndicatorLabel.Text = "!";
            outboundErrorToolTip.SetToolTip(outboundErrorIndicatorLabel, ex.Message + "\n" + ex.StackTrace);
        }
    }

    private void loadInboundSpecButton_Click(object sender, EventArgs e)
    {
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            _ = LoadSpecs(openFileDialog.FileName, inboundSpecTextBox);
        }
    }

    private void loadOutboundSpecButton_Click(object sender, EventArgs e)
    {
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            _ = LoadSpecs(openFileDialog.FileName, outboundSpecTextBox);
        }
    }
}

public class ControlWriter : TextWriter
{
    private TextBoxBase textbox;
    public ControlWriter(TextBoxBase textbox) => this.textbox = textbox;
    public override void Write(char value) => textbox.AppendText(value.ToString());
    public override void Write(string? value) => textbox.AppendText(value);
    public override Encoding Encoding => Encoding.ASCII;
}
