[PacketHandler(MessageType.ShootRequest)]
public class ShootRequestPacket : IPacket
{
    public float DirectionX { get; set; }
    public float DirectionY { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        DirectionX = buffer.ReadFloat();
        DirectionY = buffer.ReadFloat();
    }
}
