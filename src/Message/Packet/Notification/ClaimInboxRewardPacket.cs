
public class ClaimInboxRewardPacket : IPacket
{
    public int NotificationIndexId { get; set; }
    public bool Success { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.ClaimInboxRewardResponse);
        buffer.WriteVarInt(NotificationIndexId);
        buffer.WriteBool(Success);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        NotificationIndexId = buffer.ReadVarInt();
    }
}
