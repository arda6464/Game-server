public class ClubEditResponsePacket : IPacket
{
    public string ClubName { get; set; }
    public string ClubDescription { get; set; }
    public int ClubAvatarId { get; set; }
    public int AccountId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.ClubEditResponse);
        buffer.WriteVarString(ClubName);
        buffer.WriteVarString(ClubDescription);
        buffer.WriteVarInt(ClubAvatarId);
        buffer.WriteVarInt(AccountId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
