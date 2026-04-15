public class FirstConnectionResponsePacket : IPacket
{
    public bool Success { get; set; }
    public string Message { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.FirstConnectionResponse);
        buffer.WriteBool(Success);
        buffer.WriteVarString(Message);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
