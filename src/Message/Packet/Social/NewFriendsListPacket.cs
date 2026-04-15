using System.Collections.Generic;

public class NewFriendsListPacket : IPacket
{
    public List<FriendInfo> Friends { get; set; } = new List<FriendInfo>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.NewFriendsList);
        buffer.WriteVarInt(Friends.Count);
        foreach (var friend in Friends)
        {
            buffer.WriteVarInt(friend.ID); // Sayısal I
            buffer.WriteVarInt(friend.AvatarId);
            buffer.WriteVarString(friend.Username);
            buffer.WriteVarInt(friend.NameColorID);
            buffer.WriteBool(friend.IsBestFriend);
            buffer.WriteVarInt(friend.Trophy);
            buffer.WriteBool(SessionManager.IsOnline(friend.ID));
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
