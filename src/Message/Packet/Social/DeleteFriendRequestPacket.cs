[PacketHandler(MessageType.DeleteFriendRequest)]
public class DeleteFriendRequestPacket : IPacket
{
    public string TargetId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        TargetId = buffer.ReadString();
    }
}
