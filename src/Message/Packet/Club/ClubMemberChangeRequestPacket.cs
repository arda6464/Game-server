[PacketHandler(MessageType.MemberToLowerRequest)]
public class ClubMemberChangeRequestPacket : IPacket
{
    public string TargetId { get; set; }
    public short Status { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        TargetId = buffer.ReadString();
        Status = buffer.ReadShort();
    }
}
