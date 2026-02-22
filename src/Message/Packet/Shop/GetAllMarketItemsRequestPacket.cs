[PacketHandler(MessageType.GetAllMarketItemsRequest)]
public class GetAllMarketItemsRequestPacket : IPacket
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
