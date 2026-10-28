public class MatchFoundPacket : IPacket
{
    public uint Tick { get; set; }
    public int BattlePort { get; set; }
    public int PlayerCount { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.MatchFound);
        buffer.WriteUInt(Tick);
        buffer.WriteVarInt(BattlePort);
        buffer.WriteVarInt(PlayerCount);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new System.NotImplementedException();
    }
}
