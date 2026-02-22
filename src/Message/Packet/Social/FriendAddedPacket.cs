public class FriendAddedPacket : IPacket
{
    public FriendInfo? Friend { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.AcceptFriendResponse);
        buffer.WriteString(Friend?.Id);
        buffer.WriteInt(Friend.AvatarId);
        buffer.WriteString(Friend.Username);
        buffer.WriteInt(Friend.NameColorID);
        buffer.WriteBool(Friend.IsBestFriend);
        buffer.WriteInt(Friend.Trophy);
        buffer.WriteBool(SessionManager.IsOnline(Friend.Id));
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
