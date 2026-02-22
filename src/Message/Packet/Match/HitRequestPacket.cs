[PacketHandler(MessageType.HitRequest)]
public class HitRequestPacket : IPacket
{
    public string TargetId { get; set; }
    public int BulletId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        TargetId = buffer.ReadString();
        BulletId = buffer.ReadInt();
    }
}
