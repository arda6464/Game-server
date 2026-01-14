public static class FriendRequestDecline
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer byteBuffer = new ByteBuffer();
         byteBuffer.WriteBytes(message,true);
        int _ = byteBuffer.ReadInt();

        string targetId = byteBuffer.ReadString();
        byteBuffer.Dispose();
        AccountManager.AccountData account = AccountCache.Load(session.AccountId);
        AccountManager.AccountData target = AccountCache.Load(targetId);
        bool result = false;


        if (target != null)
        {
            var request = account.Requests.Find(r => r.Id == targetId);
            if (request != null)
            {
                account.Requests.Remove(request);
                result = true;
            }
                
        }
        else
        {
            Logger.errorslog("friend decline belirlenemeyen hesap buldu");
            result = false;

        }
        Console.WriteLine($"{account.Username}({account.AccountId})  adlı kullanıcı {target.Username}({target.AccountId}) adlı kullanıcının isteğini reddetti");
        using (ByteBuffer buffer = new ByteBuffer())
        {
            buffer.WriteInt((int)MessageType.DeclineFriendResponse);
            buffer.WriteString(targetId);
            buffer.WriteBool(result);
        }
    }
}