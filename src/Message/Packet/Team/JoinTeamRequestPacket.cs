[PacketHandler(MessageType.JoinTeamRequest)]
public class JoinTeamRequestPacket : IPacket
{
    public int TeamId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        TeamId = buffer.ReadVarInt();
    }
}
