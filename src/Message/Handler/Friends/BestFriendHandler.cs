[PacketHandler(MessageType.BestFriendChanged)]
public static class BestFriendHandler
{
    public static void Handle(Session session, byte[] data)
    {
        using (ByteBuffer read = new ByteBuffer())
        {
            read.WriteBytes(data);
            read.ReadShort();
            string accid = read.ReadString();
            var acc = AccountCache.Load(accid);
            if (acc == null) return;
            FriendInfo friend = session.Account.Friends.Find(f => f.Id == accid);
            if (friend == null) return;
            friend.IsBestFriend = !friend.IsBestFriend;
            Console.WriteLine($"{session.Account.Username} ({session.Account.AccountId}) {acc.Username} ({acc.AccountId}) ile artık {(friend.IsBestFriend ? "best friend" : "best friend degil")}");
        }
    }
}