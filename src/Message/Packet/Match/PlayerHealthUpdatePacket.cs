public class PlayerHealthUpdatePacket : IPacket
{
    public int PlayerID { get; set; }
    public int Health { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.PlayerHealthUpdate);
        buffer.WriteVarInt(PlayerID);
        buffer.WriteVarInt(Health);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        PlayerID = buffer.ReadVarInt();
        Health = buffer.ReadVarInt();
    }
}
