public class LeaveTeamResponsePacket : IPacket
{
    public bool Success { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.LeaveTeamResponse);
        buffer.WriteBool(Success);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
