public class ChangeNameResponsePacket : IPacket
{
    public string NewName { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.ChangeNameResponse);
        buffer.WriteString(NewName);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
