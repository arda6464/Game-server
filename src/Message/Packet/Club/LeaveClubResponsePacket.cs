public class LeaveClubResponsePacket : IPacket
{
    public bool Kicked { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.LeaveClubResponse);
        buffer.WriteBool(Kicked);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
