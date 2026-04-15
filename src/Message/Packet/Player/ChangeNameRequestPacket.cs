[PacketHandler(MessageType.ChangeNameRequest)]
public class ChangeNameRequestPacket : IPacket
{
    public string NewName { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        NewName = buffer.ReadVarString();
    }
}
