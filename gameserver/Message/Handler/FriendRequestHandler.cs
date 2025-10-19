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
        AccountManager.AccountData target = AccountCache.Load(accıd); //istek atılan kişi


        if (account != null && target != null)
        {
            // todo already request and friends control 
            FriendInfo info = new FriendInfo
            {
                Username = account.AccountId,
                Id = account.AccountId,
                AvatarId = account.Avatarid
            };
            target.Requests.Add(info);
            Logger.genellog($"{account.Username}({account.AccountId}) →  {target.Username}({target.AccountId}) 'ye istek attı");


            if (SessionManager.IsOnline(target.AccountId))
            {
                Session session1 = SessionManager.GetSession(target.AccountId);
                ByteBuffer buffer = new ByteBuffer();
                buffer.WriteInt((int)MessageType.NewRequestList);
                buffer.WriteInt(account.Requests.Count);
                foreach (var request in account.Requests)
                {
                    buffer.WriteString(request.Id);
                    buffer.WriteInt(request.AvatarId);
                    buffer.WriteString(request.Username);
                }
                byte[] veri = buffer.ToArray();
                buffer.Dispose();
                session1.Send(veri);
            }
            
            
        }
    }
}