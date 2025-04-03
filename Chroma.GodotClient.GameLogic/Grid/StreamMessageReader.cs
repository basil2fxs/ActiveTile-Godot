namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

/// <summary>
/// Finds messages in a sequential stream.
/// </summary>
public class StreamMessageReader
{
    public byte[] Header { get; init; }
    public int ExpectedMessageLength { get; init; }
    public byte[] Buffer { get; init; }
    public int BytesHeld { get; private set; }

    public delegate void MessageCallback(Span<byte> message);
    private MessageCallback _messageCallback;

    public StreamMessageReader(byte[] header, int expectedMessageLength, MessageCallback messageCallback)
    {
        Header = header;
        ExpectedMessageLength = expectedMessageLength;
        Buffer = new byte[expectedMessageLength];
        BytesHeld = 0;
        _messageCallback = messageCallback;
    }

    /// <summary>
    /// Read from the stream and append it to the buffer.
    /// Any bytes before the first header are discarded.
    /// The header will shifted to the start of the buffer.
    /// Call the message callback whenever a whole message is found.
    /// </summary>
    /// <returns>Only if the we got a zero-byte read, e.g. if the stream terminated.</returns>
    public async Task ReadStream(Stream stream, CancellationToken cancel)
    {
        while (true)
        {
            int bytesRead = await stream.ReadAsync(Buffer.AsMemory(BytesHeld, Buffer.Length - BytesHeld), cancel);
            if (bytesRead == 0) return;

            BytesHeld += bytesRead;
            int start = Buffer.AsSpan(0, BytesHeld).IndexOf(Header);
            if (start < 0)
            {
                BytesHeld = 0;
                continue;
            }
            if (start > 0)
            {
                // Shift message to start of buffer.
                Buffer.AsSpan(start, BytesHeld - start).CopyTo(Buffer);
                BytesHeld -= start;
                // Read more because, since bytes.Length == expectedMessageLength, what we have is too short.
                continue;
            }
            // So start == 0

            // Find the start of the next message.
            var headerLength = Header.Length;
            int nextStart = Buffer.AsSpan(headerLength, BytesHeld - headerLength).IndexOf(Header);
            int messageLength;
            if (nextStart < 0)
            {
                // Not found, and message not long enough, continue reading.
                if (BytesHeld < ExpectedMessageLength)
                {
                    continue;
                }
                // Not found, only take expected number of bytes.
                else
                {
                    messageLength = ExpectedMessageLength;
                }
            }
            // Found, only take up to next message.
            else
            {
                messageLength = headerLength + nextStart;
            }

            // Consume messageLength bytes
            _messageCallback(Buffer.AsSpan(start, messageLength));
            start = messageLength;

            // Shift remaining to start of buffer.
            Buffer.AsSpan(start, BytesHeld - start).CopyTo(Buffer);
            BytesHeld -= start;
        }
    }
}
