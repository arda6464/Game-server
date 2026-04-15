public class MatchMakingAddPlayerPacket : IPacket
{
    public int PlayersPerMatch { get; set; }
    public int CurrentPlayers { get; set; } = 1;

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.MatchMakingUpdate);
        buffer.WriteVarInt(PlayersPerMatch);
        buffer.WriteVarInt(CurrentPlayers);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
