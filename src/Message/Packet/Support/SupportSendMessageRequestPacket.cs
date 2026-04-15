[PacketHandler(MessageType.SupportMessageSend)]
public class SupportSendMessageRequestPacket : IPacket
{
    public int TicketNo { get; set; }
    public string?  Content { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        TicketNo = buffer.ReadVarInt();
        Content = buffer.ReadVarString();
    }
}
