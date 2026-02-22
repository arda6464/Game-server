[PacketHandler(MessageType.ClubCreateRequest)]
public class ClubCreateRequestPacket : IPacket
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
        ClubName = buffer.ReadString();
        ClubDescription = buffer.ReadString();
        AvatarId = buffer.ReadInt();
    }
}
