public class FriendAddedPacket : IPacket
{
    public FriendInfo? Friend { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.AcceptFriendResponse);
        buffer.WriteVarInt(Friend.ID);
        buffer.WriteVarInt(Friend.AvatarId);
        buffer.WriteVarString(Friend.Username);
        buffer.WriteVarInt(Friend.NameColorID);
        buffer.WriteBool(Friend.IsBestFriend);
        buffer.WriteVarInt(Friend.Trophy);
        buffer.WriteBool(SessionManager.IsOnline(Friend.ID));
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
