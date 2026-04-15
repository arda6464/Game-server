[PacketHandler(MessageType.MemberToLowerRequest)]
public class ClubMemberChangeRequestPacket : IPacket
{
    public int TargetId { get; set; }
    public int Status { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        TargetId = buffer.ReadVarInt();
        Status = buffer.ReadVarInt();
    }
}
