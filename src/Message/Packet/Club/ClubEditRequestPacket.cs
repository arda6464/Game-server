[PacketHandler(MessageType.ClubEditRequest)]
public class ClubEditRequestPacket : IPacket
{
    public string ClubName { get; set; }
    public string ClubDescription { get; set; }
    public int AvatarId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        ClubName = buffer.ReadVarString();
        ClubDescription = buffer.ReadVarString();
        AvatarId = buffer.ReadVarInt();
    }
}
