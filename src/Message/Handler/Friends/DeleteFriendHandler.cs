public static class DeleteFriendHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        int type = read.ReadInt();
        string acccountId = read.ReadString();
         read.Dispose();
        AccountManager.AccountData targetaccount = AccountCache.Load(acccountId);
        AccountManager.AccountData account = AccountCache.Load(session.AccountId);
        if (account != null && targetaccount != null)
        {
            var friends = account.Friends.Find(f => f.Id == acccountId);
            var targetfriends = targetaccount.Friends.Find(f => f.Id == session.AccountId);
            if (friends != null && targetfriends != null)
            {
                account.Friends.Remove(friends);
                targetaccount.Friends.Remove(targetfriends);
                Logger.genellog($"{account.Username}({account.AccountId}) adlı oyuncu {targetaccount.Username}({targetaccount.AccountId}) adlı oyuncuyu  arkadaşlıktan çıkardı!");

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


            if (SessionManager.IsOnline(targetaccount.AccountId))
            {
                ByteBuffer targetb = new ByteBuffer();
                Session targetsesion = SessionManager.GetSession(targetaccount.AccountId);
                targetb.WriteInt((int)MessageType.NewFriendsList);


                targetb.WriteInt(targetaccount.Friends.Count);
                foreach (var targetf in targetaccount.Friends)
                {
                    targetb.WriteString(targetf.Id);
                    targetb.WriteInt(targetf.AvatarId);
                    targetb.WriteString(targetf.Username);
                    targetb.WriteInt(targetf.NameColorID);
                    targetb.WriteBool(SessionManager.IsOnline(targetf.Id));
                }
                byte[] targetveri = targetb.ToArray();
                    targetb.Dispose();
                    targetsesion?.Send(targetveri);
                    
            }
            }
            else
            {
                Logger.genellog($"{account.Username}({account.AccountId}) {targetaccount.Username}({targetaccount.AccountId}) ile zaten arkadaş değil!");
                
            }
        }
        else
            Logger.errorslog("hesaplardan biri null");
    }
}