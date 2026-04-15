[PacketHandler(MessageType.BestFriendChanged)]
public static class BestFriendHandler
{
    public static void Handle(Session session, byte[] data)
    {
        using (ByteBuffer read = new ByteBuffer())
        {
            read.WriteBytes(data);
            read.ReadVarInt();
            int accid = read.ReadVarInt();
            var acc = AccountCache.Load(accid);
            if (acc == null) return;
            FriendInfo friend = session.Account.Friends.Find(f => f.ID == accid);
            if (friend == null) return;
            friend.IsBestFriend = !friend.IsBestFriend;
            Console.WriteLine($"{session.Account.Username} ({session.Account.ID}) {acc.Username} ({acc.ID}) ile artık {(friend.IsBestFriend ? "best friend" : "best friend degil")}");
        }
    }
}
