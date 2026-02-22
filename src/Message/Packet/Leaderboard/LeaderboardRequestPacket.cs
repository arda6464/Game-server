[PacketHandler(MessageType.LeaderboardRequest)]
public class LeaderboardRequestPacket : IPacket
{
    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // Empty
    }
}
