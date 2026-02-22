public class FriendRemovedPacket : IPacket
{
    public string TargetId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.DeleteFriendResponse);
        buffer.WriteString(TargetId);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
