public class PlayerHealthUpdatePacket : IPacket
{
    
    public int Health { get; set; }
    public int Shield {get; set;}

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.PlayerUpdateHealth);
        buffer.WriteVarInt(Health);
        buffer.WriteVarInt(Shield);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        
    }
}
