public class MatchMakingAddPlayerPacket : IPacket
{
    public int PlayersPerMatch { get; set; }
    public int CurrentPlayers { get; set; } = 1;

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.MatchMakingUpdate);
        buffer.WriteInt(PlayersPerMatch);
        buffer.WriteShort((short)CurrentPlayers);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
