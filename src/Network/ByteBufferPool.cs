using System.Collections.Concurrent;
using System.Threading;

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

        return new ByteBuffer();
    }

    public static void Return(ByteBuffer buffer)
    {
        buffer.Reset();
        buffer._returnGuard = 0;
        _pool.Add(buffer);
    }
}
