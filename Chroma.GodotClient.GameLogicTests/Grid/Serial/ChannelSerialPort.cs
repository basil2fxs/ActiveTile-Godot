using MysticClue.Chroma.GodotClient.GameLogic.Grid.Serial;
using System.Threading.Channels;

namespace MysticClue.Chroma.GodotClient.GameLogicTests.Grid.Serial;

/// <summary>
/// Fake ISerialPort that uses two channels for reading and writing.
/// Used to implement loopback ports for tests.
/// </summary>
public class ChannelSerialPortStream : Stream, ISerialPort
{
    public required Channel<byte> In { get; init; }
    public required Channel<byte> Out { get; init; }

    public Stream Stream => this;

    public async override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            var b = await In.Reader.ReadAsync(cancellationToken);
            buffer.Span[i] = b;
        }
        return buffer.Length;
    }

    public async override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < buffer.Length; i++) await Out.Writer.WriteAsync(buffer.Span[i], cancellationToken);
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
}
