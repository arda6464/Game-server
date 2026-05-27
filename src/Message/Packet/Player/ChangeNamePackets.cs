
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
