public class PlayerMovedPacket : IPacket
{
    public int AccountID { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.PlayerMoved);
        buffer.WriteVarInt(AccountID);
        buffer.WriteFloat(X);
        buffer.WriteFloat(Y);
        buffer.WriteFloat(Z);
    }


    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
