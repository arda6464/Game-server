public class KickMemberResponsePacket : IPacket
{
    public int TargetId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.KickMemberinClubResponse);
        buffer.WriteVarInt(TargetId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
