public class PlayerDeadPacket : IPacket
{
    public int DeadPlayerId { get; set; }
    public int KillerId { get; set; }
    public int GunID { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)UdpMessageType.PlayerDead);
        buffer.WriteVarInt(DeadPlayerId);
        buffer.WriteVarInt(KillerId);
        buffer.WriteVarInt(GunID);
    }

    public void Deserialize(ByteBuffer buffer)
    {
       
    }
}
