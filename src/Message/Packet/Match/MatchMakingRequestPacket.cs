[PacketHandler(MessageType.MatchMakingRequest)]
public class MatchMakingRequestPacket : IPacket
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
