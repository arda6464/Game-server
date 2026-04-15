using System;
using System.Linq;

[PacketHandler(MessageType.SendFriendRequest)]
public static class SendFriendRequestHandler
{
    public static void Handle(Session session, byte[] data)
    {
        ByteBuffer byteBuffer = new ByteBuffer();
        byteBuffer.WriteBytes(data, true);

        var request = new SendFriendRequestPacket();
        request.Deserialize(byteBuffer);

        int targetId = request.TargetId;
        byteBuffer.Dispose();
          
        AccountManager.AccountData account = session.Account;
        AccountManager.AccountData target = AccountCache.Load(targetId);

        if (account != null && target != null)
        {
            lock (account.SyncLock)
            {
                if (account.Friends.Any(f => f.ID == target.ID))
                {
                    Logger.errorslog($"{account.Username} zaten {target.Username}'nin arkadaşı");
                    return;
                }
            }

            lock (target.SyncLock)
            {
                if (target.Requests.Any(r => r.ID == account.ID))
                {
                    Logger.errorslog($"{account.Username} zaten {target.Username}'ye istek göndermiş");
                    return;
                }

                if (account.ID == target.ID)
                {
                    Logger.errorslog($"{account.Username} kendine istek atıyor");
                    return;
                }

                FriendInfo info = new FriendInfo
                {
                    ID = account.ID,
                    Username = account.Username,
                    AvatarId = account.Avatarid,
                    NameColorID = account.Namecolorid,
                    Trophy = account.Trophy
                };

                if (SessionManager.IsOnline(target.ID))
                {
                    Session targetSession = SessionManager.GetSession(target.ID);
                    if (targetSession != null)
                    {
                        var response = new FriendRequestAddedPacket { Request = info };
                        targetSession.Send(response);
                    }
                }
            
                target.Requests.Add(info);
            }
            Logger.genellog($"{account.Username}({account.ID}) → {target.Username}({target.ID}) 'ye istek attı");
        }
    }
}
