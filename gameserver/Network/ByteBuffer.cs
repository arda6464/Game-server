using System.Buffers;
using System.Text;

public sealed class ByteBuffer : IDisposable
{
    private const int DEFAULT_SIZE = 1024;
    private byte[] _buffer;
    private MemoryStream _stream;
    private BinaryWriter _writer;
    private BinaryReader _reader;
    private bool _disposed = false;

    public int Length => (int)_stream.Length;
    public int Position => (int)_stream.Position;
    public int Capacity => _buffer.Length;

    public ByteBuffer(int size = DEFAULT_SIZE)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(Math.Max(size, DEFAULT_SIZE));
        _stream = new MemoryStream(_buffer, 0, _buffer.Length, true, true);
        _writer = new BinaryWriter(_stream, Encoding.UTF8, true);
        _reader = new BinaryReader(_stream, Encoding.UTF8, true);
    }

    public void Reset()
    {
        _stream.Position = 0;
        _stream.SetLength(0);
    }

    public void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity > _buffer.Length)
        {
            int newSize = Math.Max(requiredCapacity, _buffer.Length * 2);
            Resize(newSize);
        }
    }

    private void Resize(int newSize)
    {
        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        Array.Copy(_buffer, 0, newBuffer, 0, (int)_stream.Length);

        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;

        long oldPosition = _stream.Position;
        _stream.Dispose();
        _stream = new MemoryStream(_buffer, 0, _buffer.Length, true, true);
        _stream.SetLength(oldPosition);
        _stream.Position = oldPosition;

        _writer.Dispose();
        _reader.Dispose();
        _writer = new BinaryWriter(_stream, Encoding.UTF8, true);
        _reader = new BinaryReader(_stream, Encoding.UTF8, true);
    }

    // Write methods
    public void WriteByte(byte value) => _writer.Write(value);

    public void WriteBytes(ReadOnlySpan<byte> values, bool resetPosition)
    {
        EnsureCapacity(Position + values.Length);
        _writer.Write(values);
        _stream.SetLength(_stream.Position);
        if (resetPosition) _stream.Position = 0;
    }

    public void WriteInt(int value) => _writer.Write(value);
    public void WriteUInt(uint value) => _writer.Write(value);
    public void WriteShort(short value) => _writer.Write(value);
    public void WriteUShort(ushort value) => _writer.Write(value);
    public void WriteLong(long value) => _writer.Write(value);
    public void WriteULong(ulong value) => _writer.Write(value);
    public void WriteFloat(float value) => _writer.Write(value);
    public void WriteDouble(double value) => _writer.Write(value);
    public void WriteBool(bool value) => _writer.Write(value);
    public void WriteString(string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        WriteInt(byteCount);

        EnsureCapacity(Position + byteCount);
        Span<byte> buffer = stackalloc byte[byteCount];
        Encoding.UTF8.GetBytes(value.AsSpan(), buffer);
        _writer.Write(buffer);
    }
    // Read methods
    public byte ReadByte() => _reader.ReadByte();
    public byte[] ReadBytes(byte[] message, int length)
    {
        return _reader.ReadBytes(length);
    }
    public ReadOnlySpan<byte> ReadSpan(int length)
    {
        if (_stream.Position + length > _stream.Length)
            throw new EndOfStreamException();

        var span = new ReadOnlySpan<byte>(_buffer, (int)_stream.Position, length);
        _stream.Position += length;
        return span;
    }
    public ReadOnlyMemory<byte> ReadMemory(int length)
    {
        if (_stream.Position + length > _stream.Length)
            throw new EndOfStreamException();
        var memory = new ReadOnlyMemory<byte>(_buffer, (int)_stream.Position, length);
        _stream.Position += length;
        return memory;
    }
    public int ReadInt() => _reader.ReadInt32();
    public uint ReadUInt() => _reader.ReadUInt32();
    public short ReadShort() => _reader.ReadInt16();
    public ushort ReadUShort() => _reader.ReadUInt16();
    public long ReadLong() => _reader.ReadInt64();
    public ulong ReadULong() => _reader.ReadUInt64();
    public float ReadFloat() => _reader.ReadSingle();
    public double ReadDouble() => _reader.ReadDouble();
    public bool ReadBool() => _reader.ReadBoolean();
    public string ReadString()
    {
        int length = ReadInt();
        if (length == 0) return string.Empty;

        var span = new ReadOnlySpan<byte>(_buffer, (int)_stream.Position, length);
        _stream.Position += length;

        return Encoding.UTF8.GetString(span);
    }
    public string ReadString(int length)
    {
        if (length == 0) return string.Empty;

        var span = new ReadOnlySpan<byte>(_buffer, (int)_stream.Position, length);
        _stream.Position += length;

        return Encoding.UTF8.GetString(span);
    }
    // Memory operations
    public ReadOnlySpan<byte> GetReadableSpan()
    {
        return _buffer.AsSpan((int)_stream.Position, (int)(_stream.Length - _stream.Position));
    }

    public Span<byte> GetWritableSpan()
    {
        return _buffer.AsSpan((int)_stream.Position);
    }
    public byte[] ToArray()
    {
        return _buffer.AsSpan(0, (int)_stream.Position).ToArray();
    }
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            ArrayPool<byte>.Shared.Return(_buffer);
            _stream.Dispose();
            _writer.Dispose();
            _reader.Dispose();
        }
    }
    // Finalizer for safety
    ~ByteBuffer()
    {
        Dispose();
    }
}