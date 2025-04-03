
namespace MysticClue.Chroma.GodotClient.GameLogicTests.Grid.Serial;

public class LoopbackSerialPortFactoryTest
{
    [Fact]
    public void TestMakeSerialPort()
    {
        var loopback = new LoopBackSerialPortFactory();
        var s1 = loopback.MakeSerialPort("test1");
        var s2 = loopback.MakeSerialPort("test1");
        Assert.True(s1.In != s1.Out);
        Assert.True(s1.In == s2.Out);
        Assert.True(s1.Out == s2.In);
        var s3 = loopback.MakeSerialPort("test2");
        var s4 = loopback.MakeSerialPort("test2");
        Assert.True(s3.In == s4.Out);
        Assert.True(s3.Out == s4.In);
        Assert.True(s3.In != s1.In);
        Assert.True(s3.In != s2.In);
        Assert.Throws<InvalidOperationException>(() => loopback.MakeSerialPort("test1"));
    }

    [Fact]
    public async void TestChannelSerialPort()
    {
        var loopback = new LoopBackSerialPortFactory();
        var s1 = loopback.MakeSerialPort("test1");
        var s2 = loopback.MakeSerialPort("test1");

        byte[] want = [1, 2, 3];
        await s1.WriteAsync(want);
        byte[] got = [0, 0, 0];
        Assert.Equal(3, await s2.ReadAsync(got));
        Assert.Equal(want, got);
    }
}
