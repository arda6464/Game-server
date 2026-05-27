using System.Collections.Concurrent;

public static class ByteBufferPool
{
    private static readonly ConcurrentBag<ByteBuffer> _pool = new ConcurrentBag<ByteBuffer>();

    public static ByteBuffer Get()
    {
        if (_pool.TryTake(out ByteBuffer buffer))
        {
            buffer.Reset();
            return buffer;
        }

        // Havuz boşsa yeni üret (Zamanla havuz kendi boyutunu bulacaktır)
        return new ByteBuffer();
    }

    public static void Return(ByteBuffer buffer)
    {
        // Temizle ve havuza geri koy
        buffer.Reset();
        _pool.Add(buffer);
    }
}
