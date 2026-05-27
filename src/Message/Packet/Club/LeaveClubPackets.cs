
[PacketHandler(MessageType.LeaveClubRequest)]
public class LeaveClubRequestPacket : IPacket
{
    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // No data to read
    }
}

public class LeaveClubResponsePacket : IPacket
{
    public bool Kicked { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.LeaveClubResponse);
        buffer.WriteBool(Kicked);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
