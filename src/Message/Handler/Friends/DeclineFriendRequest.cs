[PacketHandler(MessageType.DeclineFriendRequest)]
public static class FriendRequestDecline
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer byteBuffer = new ByteBuffer();
         byteBuffer.WriteBytes(message,true);

         var request = new FriendRequestDeclinePacket();
        request.Deserialize(byteBuffer);
        
        int targetId = request.TargetId;
        byteBuffer.Dispose();
        if (session.Account == null) return;
        AccountManager.AccountData account = session.Account;
        AccountManager.AccountData target = AccountCache.Load(targetId);
        bool result = false;


        if (target != null)
        {
            lock (account.SyncLock)
            {
                var req = account.Requests.Find(r => r.ID == targetId);
                if (req != null)
                {
                    account.Requests.Remove(req);
                    result = true;
                }
            }
                
        }
        else
        {
            Logger.errorslog("friend decline belirlenemeyen hesap buldu");
            result = false;

        }
        Console.WriteLine($"{account.Username}({account.ID})  adlı kullanıcı {target.Username}({target.ID}) adlı kullanıcının isteğini reddetti");
        
        
    }
}
