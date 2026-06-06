
public class PlayerGivedItemPacket : IPacket
{
   public int playerId { get; set; }
   public int SlotId { get; set; }
    public int ItemType { get; set; }
    public int DataId { get; set; }
    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.ItemAdded);
        buffer.WriteVarInt(playerId);
        buffer.WriteVarInt(SlotId);
        buffer.WriteVarInt(ItemType);
        buffer.WriteVarInt(DataId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
       
    }
}
