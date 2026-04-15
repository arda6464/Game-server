[PacketHandler(MessageType.SendTeamMessageRequest)]
public class SendTeamMessageRequestPacket : IPacket
{
    public string Message { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        Message = buffer.ReadVarString();
    }
}
