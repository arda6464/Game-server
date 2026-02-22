[PacketHandler(MessageType.MatchMakingCancelRequest)]
public class MatchMakingCancelRequestPacket : IPacket
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
