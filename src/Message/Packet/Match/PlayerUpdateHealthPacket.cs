public class PlayerUpdateHealthPacket : IPacket
{
    public int NewHealth { get; set; }
    public int NewShield { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.PlayerUpdateHealth);
        buffer.WriteVarInt(NewHealth);
        buffer.WriteVarInt(NewShield);

    }

    public void Deserialize(ByteBuffer buffer)
    {

    }
}
