public static class FriendRequestAccept
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer byteBuffer = new ByteBuffer();
        byteBuffer.WriteBytes(message, true);
        int _ = byteBuffer.ReadInt();

        string targetId = byteBuffer.ReadString();
        byteBuffer.Dispose();


        AccountManager.AccountData account = AccountCache.Load(session.AccountId);
        AccountManager.AccountData target = AccountCache.Load(targetId);


        if (target != null)
        {
            var request = account.Requests.Find(r => r.Id == targetId);
            if (request != null)
                account.Requests.Remove(request);
        }

        FriendInfo friend1 = new FriendInfo()
        {
            Username = account.Username,
            AvatarId = account.Avatarid,
            Id = account.AccountId,
            NameColorID = account.Namecolorid
        };

        FriendInfo friend2 = new FriendInfo()
        {
            Username = target.Username,
            AvatarId = target.Avatarid,
            Id = target.AccountId,
            NameColorID = target.Namecolorid
        };
        account.Friends.Add(friend2);
        target.Friends.Add(friend1);
        Console.WriteLine($"{account.Username}({account.AccountId})  adlı kullanıcı {target.Username}({target.AccountId}) adlı kullanıcının isteğini kabul etti");
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.NewFriendsList);

        buffer.WriteInt(account.Friends.Count);
        foreach (var friend in account.Friends)
        {
            buffer.WriteString(friend.Id);
            buffer.WriteInt(friend.AvatarId);
            buffer.WriteString(friend.Username);
            buffer.WriteInt(friend.NameColorID);
            buffer.WriteBool(SessionManager.IsOnline(friend.Id));
        }
        byte[] veri = buffer.ToArray();
        buffer.Dispose();
        session.Send(veri);
        if (SessionManager.IsOnline(targetId))
        {
            ByteBuffer targetb = new ByteBuffer();
            Session targetsesion = SessionManager.GetSession(targetId);
            targetb.WriteInt((int)MessageType.NewFriendsList);
            targetb.WriteInt(target.Friends.Count);
            foreach (var targetf in target.Friends)
            {
                targetb.WriteString(targetf.Id);
                targetb.WriteInt(targetf.AvatarId);
                targetb.WriteString(targetf.Username);
                targetb.WriteInt(targetf.NameColorID);
                buffer.WriteBool(SessionManager.IsOnline(targetf.Id));
                byte[] targetveri = targetb.ToArray();
                targetb.Dispose();
                targetsesion.Send(targetveri);
            }

        }

    }
}