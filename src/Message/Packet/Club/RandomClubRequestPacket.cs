[PacketHandler(MessageType.GetRandomClubRequest)]
public class RandomClubRequestPacket : IPacket
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
