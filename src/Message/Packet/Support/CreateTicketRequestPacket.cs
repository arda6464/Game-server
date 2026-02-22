[PacketHandler(MessageType.SupporCreateTicketRequest)]
public class CreateTicketRequestPacket : IPacket
{
    public byte ReasonType { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        ReasonType = buffer.ReadByte();
    }
}
