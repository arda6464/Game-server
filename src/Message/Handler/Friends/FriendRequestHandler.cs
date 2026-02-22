using System;

[PacketHandler(MessageType.SendFriendRequest)]
public static class SendFriendRequestHandler
{
    public static void Handle(Session session, byte[] data)
    {
        ByteBuffer byteBuffer = new ByteBuffer();
        byteBuffer.WriteBytes(data, true);
        int _ = byteBuffer.ReadShort();

        var request = new SendFriendRequestPacket();
        request.Deserialize(byteBuffer);

        string accıd = request.TargetId;
        byteBuffer.Dispose();
          
        AccountManager.AccountData account = session.Account;
        AccountManager.AccountData target = AccountCache.Load(accıd); //istek atılan kişi


        if (account != null && target != null)
        {
            lock (account.SyncLock)
            {
                // Zaten arkadaş mı?
                if (account.Friends.Any(f => f.Id == accıd))
                {
                    Logger.errorslog($"{account.Username} zaten {target.Username}'nin arkadaşı");
                    return;
                }
            }

            lock (target.SyncLock)
            {
                if (target.Requests.Any(r => r.Id == account.AccountId))
                {
                    Logger.errorslog($"{account.Username} zaten {target.Username}'ye istek göndermiş");
                    return;
                }

                // Kendine istek atamaz
                if (account.AccountId == accıd)
                {
                    Logger.errorslog($"{account.Username} kendine istek atıyor");
                    return;
                }

                FriendInfo info = new FriendInfo
                {
                    Username = account.Username,
                    Id = account.AccountId,
                    AvatarId = account.Avatarid,
                    NameColorID = account.Namecolorid
                };
                 if (SessionManager.IsOnline(target.AccountId))
            {
                Session targetSession = SessionManager.GetSession(target.AccountId);
                if (targetSession != null)
                {
                    var response = new FriendRequestAddedPacket { Request = info };
                    targetSession.Send(response);
                }
            }
            
                target.Requests.Add(info);
            }
            Logger.genellog($"{account.Username}({account.AccountId}) →  {target.Username}({target.AccountId}) 'ye istek attı");


           
            
        }
    }
}