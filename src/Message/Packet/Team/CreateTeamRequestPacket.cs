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
