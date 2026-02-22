public class ClubEditResponsePacket : IPacket
{
    public string ClubName { get; set; }
    public string ClubDescription { get; set; }
    public int ClubAvatarId { get; set; }
    public string AccountId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.ClubEditResponse);
        buffer.WriteString(ClubName);
        buffer.WriteString(ClubDescription);
        buffer.WriteInt(ClubAvatarId);
        buffer.WriteString(AccountId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
