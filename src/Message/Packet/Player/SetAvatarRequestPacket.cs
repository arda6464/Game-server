[PacketHandler(MessageType.SetAvatarRequest)]
public class SetAvatarRequestPacket : IPacket
{
    public int AvatarId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        AvatarId = buffer.ReadInt();
    }
}
