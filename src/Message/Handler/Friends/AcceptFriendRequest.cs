public static class FriendRequestAccept
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer byteBuffer = new ByteBuffer();
        byteBuffer.WriteBytes(message, true);
        int _ = byteBuffer.ReadInt();

        string targetId = byteBuffer.ReadString();
        byteBuffer.Dispose();


        AccountManager.AccountData account = AccountCache.Load(session.AccountId); // isteği kabul eden kişi
        AccountManager.AccountData target = AccountCache.Load(targetId); // isteği kabul edilen kişi
        try
        {

            if (target != null)
            {
                var request = account.Requests.Find(r => r.Id == targetId);
                if (request != null)
                    account.Requests.Remove(request);
                else
                {
                    Logger.errorslog($"{request?.Username} adlı istek bulunamadı");
                    return;
                } 

            }
            else
            {
                Logger.errorslog($"[Friend manager] {targetId}'li hesap bulunamadı");
                return;
            }

           // account'ın listesine target'ın bilgilerini ekle
            FriendInfo friendForAccount = new FriendInfo()
            {
                Username = target.Username,
                AvatarId = target.Avatarid,
                Id = target.AccountId,
                NameColorID = target.Namecolorid
            };

            // target'ın listesine account'ın bilgilerini ekle
            FriendInfo friendForTarget = new FriendInfo()
            {
                Username = account.Username,
                AvatarId = account.Avatarid,
                Id = account.AccountId,
                NameColorID = account.Namecolorid
            };

            account.Friends.Add(friendForAccount);
            target.Friends.Add(friendForTarget);
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
                    targetb.WriteBool(SessionManager.IsOnline(targetf.Id));
                }
                byte[] targetveri = targetb.ToArray();
                    targetb.Dispose();
                    targetsesion?.Send(targetveri);
                    
            }
        }
        catch (Exception ex)
        {
            Logger.errorslog("acceptfriends hata: " + ex.Message + "TAM HATA: " + ex.ToString());
        }


    }
}