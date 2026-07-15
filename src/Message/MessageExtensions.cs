public static class MessageExtensions
{
    public static T DeserializePacket<T>(this byte[]? data) where T : IPacket, new()
    {
        using var buffer = ByteBufferPool.Get();
        buffer.WriteBytes(data);
        var packet = new T();
        packet.Deserialize(buffer);
        return packet;
    }
}
