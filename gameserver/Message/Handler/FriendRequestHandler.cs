using System;

public static class SendFriendRequestHandler
{
    public static void Handle(Session session, byte[] data)
    {
        ByteBuffer byteBuffer = new ByteBuffer();
        byteBuffer.WriteBytes(data, true);
        int _ = byteBuffer.ReadInt();


        string accıd = byteBuffer.ReadString();
      

        AccountManager.AccountData account = AccountCache.Load(session.AccountId);
        AccountManager.AccountData target = AccountCache.Load(accıd);


        if (account != null && target != null)
        {
            // todo already request and friends control 
            FriendInfo info = new FriendInfo
            {
                Username = target.AccountId,
                Id = target.AccountId,
                AvatarId = target.Avatarid
            };
            account.Requests.Add(info);
            Logger.genellog($"{target.Username}({target.AccountId}) → {account.Username}({account.AccountId})'ye istek attı");

            ByteBuffer buffer = new ByteBuffer();
            if (SessionManager.IsOnline(account.AccountId))
            {
                
            }
            
        }
    }
}