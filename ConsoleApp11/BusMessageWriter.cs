using BusMessageLibrary;

public class BusMessageWriter
{
    private readonly IBusConnection _connection;
    private readonly MemoryStream _buffer = new();

    public BusMessageWriter(IBusConnection connection)
    {
        _connection = connection;
    }

    public async Task SendMessageAsync(byte[] nextMessage)
    {
        var safeStream = Stream.Synchronized(_buffer);
        await safeStream.WriteAsync(nextMessage, 0, nextMessage.Length);

        if (safeStream.Length >= Constants.BufferThreshold)
        {
            await _connection.PublishAsync(_buffer.ToArray());
            safeStream.SetLength(0);
        }
    }
}