public class CreateTeamResponsePacket : IPacket
{
    public int TeamId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.CreateTeamResponse);
        buffer.WriteInt(TeamId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
