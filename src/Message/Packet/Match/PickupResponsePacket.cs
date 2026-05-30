
public class PickupResponsePacket : IPacket
{
   public int LootID { get; set; }
   public bool Success { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.PickupResponse);
        buffer.WriteVarInt(LootID);
        buffer.WriteBool(Success);
    }

    public void Deserialize(ByteBuffer buffer)
    {
       
    }
}
