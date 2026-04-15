public class PlayerDeadPacket : IPacket
{
    public int DeadPlayerId { get; set; }
    public int KillerId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.PlayerDead);
        buffer.WriteVarInt(DeadPlayerId);
        buffer.WriteVarInt(KillerId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        DeadPlayerId = buffer.ReadVarInt();
        KillerId = buffer.ReadVarInt();
    }
}
