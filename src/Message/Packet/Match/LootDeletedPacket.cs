
public class LootDeletedPacket : IPacket
{
   public int LootId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.LootTaken);
        buffer.WriteVarInt(LootId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
       
    }
}
