public class ChangeNameResponsePacket : IPacket
{
    public string NewName { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.ChangeNameResponse);
        buffer.WriteVarString(NewName);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
