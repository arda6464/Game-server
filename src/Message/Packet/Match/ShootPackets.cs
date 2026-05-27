
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

public class ShootResponsePacket : IPacket
{
    public int OwnerID { get; set; }
    public int BulletId { get; set; }
    public float Speed { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float DirectionX { get; set; }
    public float DirectionY { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.Shoot);
        buffer.WriteVarInt(OwnerID);
        buffer.WriteVarInt(BulletId);
        buffer.WriteFloat(Speed);
        buffer.WriteFloat(PositionX);
        buffer.WriteFloat(PositionY);
        buffer.WriteFloat(PositionZ);
        buffer.WriteFloat(DirectionX);
        buffer.WriteFloat(DirectionY);
    }


    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
