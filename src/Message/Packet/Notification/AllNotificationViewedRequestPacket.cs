[PacketHandler(MessageType.AllNotficationViewed)]
public class AllNotificationViewedRequestPacket : IPacket
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
