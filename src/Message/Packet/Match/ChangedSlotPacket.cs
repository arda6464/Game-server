
public class ChangedSlotPacket : IPacket
{
   public int PlayerId { get; set; }
    public int ToSlot { get; set; }
    public LootItemType Itemtype { get; set; } 
    public int DataId { get; set; } 

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.ChangeSlot);
        buffer.WriteVarInt(PlayerId);
        buffer.WriteVarInt(ToSlot);
        buffer.WriteVarInt((int)Itemtype);
        buffer.WriteVarInt(DataId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
       
    }
}
