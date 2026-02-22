[PacketHandler(MessageType.ShowProfileRequest)]
public class ShowProfileRequestPacket : IPacket
{
    public string AccountId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        AccountId = buffer.ReadString();
    }
}
