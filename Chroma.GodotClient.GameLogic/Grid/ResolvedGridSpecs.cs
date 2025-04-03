using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using System.Drawing;
using System.Net;
using System.Runtime.CompilerServices;

namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

/// <summary>
/// Resolve GridSpecs into data structures that can be used to map game output to hardware.
/// </summary>
public class ResolvedGridSpecs
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int PixelsPerUnit { get; private set; }

    public bool UseSerial { get; private set; }
    public IPEndPoint[] OutputPorts { get; private set; }
    public string[] SerialPorts { get; private set; }

    private (byte, byte)[,] _chainFromGrid;
    private (byte, byte)[][] _gridFromChain;

    public (byte, byte) ChainFromGrid(int x, int y) => _chainFromGrid[x, y];
    public ReadOnlySpan<(byte, byte)> GridFromChain(int i) => _gridFromChain[i];
    public int RgbMessageLength(int i) => 2 + 3 * GridFromChain(i).Length;
    public int SensorMessageLength(int i) => 2 + GridFromChain(i).Length;
    private byte[] _rgbHeader = [0xFF, 0xFF];
    private byte[][] _sensorHeader;
    public ReadOnlySpan<byte> RgbHeader => _rgbHeader;
    public ReadOnlySpan<byte> SensorHeader(int i) => _sensorHeader[i];
    // What the sensors return when they are (un)pressed.
    public static byte SensorValuePressed => 0x0A;
    public static byte SensorValueUnpressed => 0x05;

    public ResolvedGridSpecs(GridSpecs specs)
    {
        int chainCount = specs.OutputChains?.Count ?? 0;
        if (chainCount > 0)
        {
            if (specs.ColumnWise) { throw new NotImplementedException("ColumnWise not supported yet."); }
            if (specs.PixelsPerUnit != 1) { throw new NotImplementedException("PixelsPerUnit > 1 not supported with OutputChain yet."); }
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(specs.Width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(specs.Height, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(specs.PixelsPerUnit, 1);
        UseSerial = false;
        bool allSerial = true;
        var portException = new ArgumentException("All OutputChains must all set Endpoint or all set SerialPort.");
        for (int i = 0; i < chainCount; ++i)
        {
            var chain = specs.OutputChains![i];
            if (chain.Endpoint != null)
            {
                if (chain.SerialPort != null) throw portException;
                else allSerial = false;
            }
            else
            {
                if (chain.SerialPort != null)
                {
                    UseSerial = true;
                    for (int j = 0; j < i; ++j)
                    {
                        if (chain.SerialPort == specs.OutputChains[j].SerialPort)
                        {
                            throw new ArgumentException($"Duplicate serial port: {chain}");
                        }
                    }
                }
                else throw portException;
            }
        }
        if (UseSerial && !allSerial) throw portException;

        Width = specs.Width;
        Height = specs.Height;
        PixelsPerUnit = specs.PixelsPerUnit;
        OutputPorts = new IPEndPoint[chainCount];
        SerialPorts = new string[chainCount];
        _chainFromGrid = new (byte, byte)[Width, Height];
        _gridFromChain = new (byte, byte)[chainCount][];
        _sensorHeader = new byte[chainCount][];
        for (int i = 0; i < chainCount; ++i)
        {
            var oc = specs.OutputChains![i];
            if (UseSerial)
            {
                if (oc.SerialPort == null)
                    throw new InvalidOperationException("ResolvedGridSpecs validation incorrect.");
                SerialPorts[i] = oc.SerialPort;
            }
            else
            {
                if (oc.Endpoint == null)
                    throw new InvalidOperationException("ResolvedGridSpecs validation incorrect.");
                OutputPorts[i] = oc.Endpoint;
            }

            var linesInChain = Math.Abs(oc.FirstIndex - oc.LastIndex) + 1;
            _gridFromChain[i] = new (byte, byte)[specs.Width * linesInChain];
            byte t = 0;
            int x = oc.ConnectedAtEnd ? Width - 1 : 0;
            int xDir = oc.ConnectedAtEnd ? -1 : 1;
            int yDir = Math.Sign(oc.LastIndex - oc.FirstIndex);
            for (int y = oc.FirstIndex; ; y += yDir)
            {
                for (int w = 0; w < Width; ++w)
                {
                    _chainFromGrid[x, y] = ((byte)i, t);
                    _gridFromChain[i][t] = ((byte)x, (byte)y);
                    x += xDir;
                    ++t;
                }
                xDir *= -1;
                x += xDir;
                if (y == oc.LastIndex) { break; }
            }
            _sensorHeader[i] = [0xFC, (byte)_gridFromChain[i].Length];
        }
    }

    private static int Clamp(float value, int max, [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        var valueInt = (int)value;
        Assert.Clamp(ref valueInt, 0, max, paramName);
        return valueInt;
    }
    public int ToGridX(float x) => Clamp(x, Width - 1);
    public int ToGridY(float y) => Clamp(y, Height - 1);

    public bool PointWithinGrid(Point p) => p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;
}
