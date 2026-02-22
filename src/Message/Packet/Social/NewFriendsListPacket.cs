using System.Collections.Generic;

public class NewFriendsListPacket : IPacket
{
    public List<FriendInfo> Friends { get; set; } = new List<FriendInfo>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.NewFriendsList);
        buffer.WriteInt(Friends.Count);
        foreach (var friend in Friends)
        {
            buffer.WriteString(friend.Id);
            buffer.WriteInt(friend.AvatarId);
            buffer.WriteString(friend.Username);
            buffer.WriteInt(friend.NameColorID);
            buffer.WriteBool(friend.IsBestFriend);
            buffer.WriteInt(friend.Trophy);
            buffer.WriteBool(SessionManager.IsOnline(friend.Id));
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
