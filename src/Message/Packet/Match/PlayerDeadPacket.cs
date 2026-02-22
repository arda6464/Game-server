public class PlayerDeadPacket : IPacket
{
    public string DeadPlayerId { get; set; }
    public string KillerId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.PlayerDead);
        buffer.WriteString(DeadPlayerId);
        buffer.WriteString(KillerId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
