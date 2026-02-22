public class PlayerMovedPacket : IPacket
{
    public string AccountId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.PlayerMoved);
        buffer.WriteString(AccountId);
        buffer.WriteFloat(X);
        buffer.WriteFloat(Y);
        buffer.WriteFloat(Z);
    }


    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
