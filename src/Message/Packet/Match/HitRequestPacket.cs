[PacketHandler(MessageType.HitRequest)]
public class HitRequestPacket : IPacket
{
    public int TargetID { get; set; }
    public int BulletId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.HitRequest);
        buffer.WriteVarInt(TargetID);
        buffer.WriteVarInt(BulletId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        TargetID = buffer.ReadVarInt();
        BulletId = buffer.ReadVarInt();
    }
}
