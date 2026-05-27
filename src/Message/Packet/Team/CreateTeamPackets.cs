
[PacketHandler(MessageType.CreateTeamRequest)]
public class CreateTeamRequestPacket : IPacket
{
    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // No data
    }
}

public class CreateTeamResponsePacket : IPacket
{
    public int TeamId { get; set; }
    public string Link {get; set;}

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.CreateTeamResponse);
        buffer.WriteVarInt(TeamId);
        buffer.WriteVarString(Link);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
