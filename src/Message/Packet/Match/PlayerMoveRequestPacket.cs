[PacketHandler(MessageType.Move)]
public class PlayerMoveRequestPacket : IPacket
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        X = buffer.ReadFloat();
        Y = buffer.ReadFloat();
        Z = buffer.ReadFloat();
    }

}
