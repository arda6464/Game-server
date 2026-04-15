public class FriendRequestAddedPacket : IPacket
{
    public FriendInfo Request { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.NewRequestList); // We can still use the same ID if the client can handle single item vs list, but better to use a dedicated incremental structure if client allows.

       buffer.WriteVarInt(Request.ID);
        buffer.WriteVarInt(Request.AvatarId);
        buffer.WriteVarString(Request.Username);
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
