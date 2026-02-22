public class FriendRequestAddedPacket : IPacket
{
    public FriendInfo Request { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.NewRequestList); // We can still use the same ID if the client can handle single item vs list, but better to use a dedicated incremental structure if client allows.

        buffer.WriteString(Request.Id);
        buffer.WriteInt(Request.AvatarId);
        buffer.WriteString(Request.Username);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
