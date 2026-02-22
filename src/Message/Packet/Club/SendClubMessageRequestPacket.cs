[PacketHandler(MessageType.SendClubMessage)]
public class SendClubMessageRequestPacket : IPacket
{
    public string AccountId { get; set; }
    public string Message { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        AccountId = buffer.ReadString();
        Message = buffer.ReadString();
    }
}
