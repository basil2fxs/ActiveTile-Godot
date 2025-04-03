using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.GameLogicTests.Grid.Serial;
using System.Net;
using System.Net.Sockets;

namespace MysticClue.Chroma.GodotClient.GameLogicTests.Grid;

[Collection("Sequential")]
public class NetworkInterfaceTests
{
    public static int GetAvailablePort(ProtocolType protocol)
    {
        using (var socket = new Socket(AddressFamily.InterNetwork, protocol == ProtocolType.Udp ? SocketType.Dgram : SocketType.Stream, protocol))
        {
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            if (socket.LocalEndPoint == null)
            {
                throw new InvalidOperationException("Could not get available port");
            }
            return ((IPEndPoint)socket.LocalEndPoint).Port;
        }
    }

    private sealed class FakeImage : IFrameView
    {
        public byte[] Pixels;
        public int UpdateCount;

        private int _width;

        public FakeImage(int width, int height)
        {
            Pixels = new byte[width * height * 3];
            _width = width;
        }

        public FakeImage(string hex, int width)
        {
            Pixels = Convert.FromHexString(hex);
            _width = width;
        }

        public void UpdatePixel(int x, int y, byte r, byte g, byte b)
        {
            int i = 3 * (x + _width * y);
            Pixels[i] = r;
            Pixels[i + 1] = g;
            Pixels[i + 2] = b;
            // This means the server has received something and tried to update this, notify the test task.
            ++UpdateCount;
        }

        public (byte, byte, byte) GetPixel(int x, int y)
        {
            int i = 3 * (x + _width * y);
            return (Pixels[i], Pixels[i + 1], Pixels[i + 2]);
        }

        public override string ToString() => Convert.ToHexString(Pixels.AsSpan());
    }

    [Fact]
    public void TestFakePixel()
    {
        var initialImage =
            "0102030405060708090A0B0C" +
            "1112131415161718191A1B1C" +
            "2122232425262728292A2B2C";
        var src = new FakeImage(initialImage, 4);
        Assert.Equal((0x17, 0x18, 0x19), src.GetPixel(2, 1));
        src.UpdatePixel(2, 1, 0x17, 0x18, 0x19);
        Assert.Equal(1, src.UpdateCount);
        Assert.Equal(initialImage, src.ToString());
    }

    private sealed class FakeSensorData : ISensorView
    {
        public byte[] Tiles;
        public int UpdateCount;

        private int _width;

        public FakeSensorData(int width, int height)
        {
            Tiles = new byte[width * height];
            _width = width;
        }

        public FakeSensorData(string hex, int width)
        {
            Tiles = Convert.FromHexString(hex);
            _width = width;
        }

        public void UpdateSensorData(int x, int y, byte sensorData)
        {
            int i = x + _width * y;
            Tiles[i] = sensorData;
            // This means the server has received something and tried to update this, notify the test task.
            ++UpdateCount;
        }

        public byte GetTile(int x, int y)
        {
            return Tiles[x + _width * y];
        }

        public override string ToString() => Convert.ToHexString(Tiles.AsSpan());
    }

    [Fact]
    public void TestFakeSensorData()
    {
        var initialSensorData =
            "01020304" +
            "11121314" +
            "21222324";
        var src = new FakeSensorData(initialSensorData, 4);
        Assert.Equal(0x13, src.GetTile(2, 1));
        src.UpdateSensorData(2, 1, 0x13);
        Assert.Equal(1, src.UpdateCount);
        Assert.Equal(initialSensorData, src.ToString());
    }

    [Fact]
    public async Task TestTcpRoundTrip()
    {
        var endpointFactory = () => new IPEndPoint(IPAddress.Loopback, GetAvailablePort(ProtocolType.Tcp));
        var (resolvedSpecs, dst) = PrepareTestRoundTrip(endpointFactory);
        using var hardwareSide = new TcpHardwareGameInterface(dst.UpdatePixel);
        using var gameSide = new TcpGameHardwareInterface(resolvedSpecs);
        await TestRoundTrip(resolvedSpecs, dst, gameSide, hardwareSide);
    }

    [Fact]
    public async Task TestUdpRoundTrip()
    {
        var endpointFactory = () => new IPEndPoint(IPAddress.Loopback, GetAvailablePort(ProtocolType.Udp));
        var endpoint = endpointFactory();
        var (resolvedSpecs, dst) = PrepareTestRoundTrip(endpointFactory);
        using var hardwareSide = new UdpHardwareGameInterface(endpoint, dst.UpdatePixel);
        using var gameSide = new UdpGameHardwareInterface(resolvedSpecs, endpoint);
        await TestRoundTrip(resolvedSpecs, dst, gameSide, hardwareSide);
    }

    [Fact]
    public async Task TestSerialRoundTrip()
    {
        var serialPortFactory = new LoopBackSerialPortFactory();
        var (resolvedSpecs, dst) = PrepareTestRoundTrip(useSerial: true);
        using var hardwareSide = new SerialHardwareGameInterface(dst.UpdatePixel, serialPortFactory);
        using var gameSide = new SerialGameHardwareInterface(resolvedSpecs, serialPortFactory);
        await TestRoundTrip(resolvedSpecs, dst, gameSide, hardwareSide);
    }

    private static (ResolvedGridSpecs, FakeImage) PrepareTestRoundTrip(
        Func<IPEndPoint>? endpoint = null,
        bool useSerial = false)
    {
        var gridSpecs = new GridSpecs(
            Width: 4,
            Height: 3,
            PixelsPerUnit: 1,
            ColumnWise: false,
            OutputChains: [
                new(
                    Endpoint: endpoint?.Invoke(),
                    SerialPort: useSerial ? "test-serial-port-1" : null,
                    ConnectedAtEnd: true,
                    FirstIndex: 0,
                    LastIndex: 1
                ),
                new(endpoint?.Invoke(), useSerial? "test-serial-port-2": null, false, 2, 2),
            ]
        );
        var resolvedSpecs = new ResolvedGridSpecs(gridSpecs);
        var dst = new FakeImage(resolvedSpecs.Width, resolvedSpecs.Height);
        return (resolvedSpecs, dst);
    }

    private static async Task TestRoundTrip(ResolvedGridSpecs resolvedSpecs, FakeImage dst, IGameHardwareInterface gameSide, IHardwareGameInterface hardwareSide)
    {
        var initialImage =
            "0102030405060708090A0B0C" +
            "1112131415161718191A1B1C" +
            "2122232425262728292A2B2C";
        var src = new FakeImage(initialImage, 4);
        hardwareSide.UpdateGrid(resolvedSpecs);
        var timeout = Task.Delay(5000);
        while (!gameSide.AllConnected)
        {
            var res = await Task.WhenAny(Task.Delay(100), timeout);
            if (res == timeout)
            {
                throw new TimeoutException("sender not connected");
            }
        }
        await gameSide.PutFrame(src);
        int expectedUpdates = resolvedSpecs.Width * resolvedSpecs.Height;
        while (dst.UpdateCount != expectedUpdates)
        {
            var res = await Task.WhenAny(Task.Delay(100), timeout);
            if (res == timeout)
            {
                throw new TimeoutException($"dst not updated: want {expectedUpdates} got {dst.UpdateCount}");
            }
        }
        Assert.Equal(initialImage, dst.ToString());
    }

    [Fact]
    public async Task TestUdpSensorRoundTrip()
    {
        var endpointFactory = () => new IPEndPoint(IPAddress.Loopback, GetAvailablePort(ProtocolType.Udp));
        var endpoint = endpointFactory();
        var (resolvedSpecs, dst) = PrepareTestSensorRoundTrip(endpointFactory);
        var unusedImage = new FakeImage(resolvedSpecs.Width, resolvedSpecs.Height);
        using var hardwareSide = new UdpHardwareGameInterface(endpoint, unusedImage.UpdatePixel);
        using var gameSide = new UdpGameHardwareInterface(resolvedSpecs, endpoint);
        gameSide.UpdateSensorCallback = dst.UpdateSensorData;
        await TestSensorRoundTrip(resolvedSpecs, dst, gameSide, hardwareSide);
    }

    [Fact]
    public async Task TestSerialSensorRoundTrip()
    {
        var serialPortFactory = new LoopBackSerialPortFactory();
        var (resolvedSpecs, dst) = PrepareTestSensorRoundTrip(useSerial: true);
        var unusedImage = new FakeImage(resolvedSpecs.Width, resolvedSpecs.Height);
        using var hardwareSide = new SerialHardwareGameInterface(unusedImage.UpdatePixel, serialPortFactory);
        using var gameSide = new SerialGameHardwareInterface(resolvedSpecs, serialPortFactory);
        gameSide.UpdateSensorCallback = dst.UpdateSensorData;
        await TestSensorRoundTrip(resolvedSpecs, dst, gameSide, hardwareSide);
    }

    private static (ResolvedGridSpecs, FakeSensorData) PrepareTestSensorRoundTrip(
        Func<IPEndPoint>? endpoint = null,
        bool useSerial = false)
    {
        var gridSpecs = new GridSpecs(
            Width: 4,
            Height: 3,
            PixelsPerUnit: 1,
            ColumnWise: false,
            OutputChains: [
                new(
                    Endpoint: endpoint?.Invoke(),
                    SerialPort: useSerial ? "test-serial-port-1" : null,
                    ConnectedAtEnd: true,
                    FirstIndex: 0,
                    LastIndex: 1
                ),
                new(endpoint?.Invoke(), useSerial ? "test-serial-port-2" : null, false, 2, 2),
            ]
        );
        var resolvedSpecs = new ResolvedGridSpecs(gridSpecs);
        var dst = new FakeSensorData(resolvedSpecs.Width, resolvedSpecs.Height);
        return (resolvedSpecs, dst);
    }

    private static async Task TestSensorRoundTrip(
        ResolvedGridSpecs resolvedSpecs,
        FakeSensorData dst,
        IGameHardwareInterface gameSide,
        IHardwareGameInterface hardwareSide)
    {
        var initialSensorData =
            "01020304" +
            "11121314" +
            "21222324";
        var src = new FakeSensorData(initialSensorData, 4);
        hardwareSide.UpdateGrid(resolvedSpecs);
        var timeout = Task.Delay(5000);
        while (!gameSide.AllConnected)
        {
            var res = await Task.WhenAny(Task.Delay(100), timeout);
            if (res == timeout)
            {
                throw new TimeoutException("sender not connected");
            }
        }
        {
            var res = await Task.WhenAny(hardwareSide.PutSensorData(src), timeout);
            if (res == timeout)
            {
                throw new TimeoutException($"PutSensorData() timed out");
            }
        }
        int expectedUpdates = resolvedSpecs.Width * resolvedSpecs.Height;
        while (dst.UpdateCount != expectedUpdates)
        {
            var res = await Task.WhenAny(Task.Delay(100), timeout);
            if (res == timeout)
            {
                throw new TimeoutException($"dst not updated: want {expectedUpdates} got {dst.UpdateCount}");
            }
        }
        Assert.Equal(initialSensorData, dst.ToString());
    }
}
