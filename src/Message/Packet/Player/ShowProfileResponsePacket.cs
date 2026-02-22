using Logic;

public class ShowProfileResponsePacket : IPacket
{
    public string AccountId { get; set; }
    public string Username { get; set; }
    public int NameColorId { get; set; }
    public int AvatarId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.ShowProfileResponse);
        buffer.WriteString(AccountId);
        buffer.WriteString(Username);
        buffer.WriteInt(NameColorId);
        buffer.WriteInt(AvatarId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
