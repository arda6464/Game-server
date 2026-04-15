[PacketHandler(MessageType.DeleteFriendRequest)]
public static class DeleteFriendHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        
        var request = new DeleteFriendRequestPacket();
        request.Deserialize(read);
        
        int targetId = request.TargetId;
         read.Dispose();
        if (session.Account == null) return;
        AccountManager.AccountData targetaccount = AccountCache.Load(targetId);
        AccountManager.AccountData account = session.Account;
        if (account != null && targetaccount != null)
        {
            lock (account.SyncLock)
            lock (targetaccount.SyncLock)
            {
                var friend = account.Friends.Find(f => f.ID == targetId);
                var targetFriend = targetaccount.Friends.Find(f => f.ID == account.ID);
                if (friend != null && targetFriend != null)
                {
                    account.Friends.Remove(friend);
                    targetaccount.Friends.Remove(targetFriend);
                    Logger.genellog($"{account.Username}({account.ID}) adlı oyuncu {targetaccount.Username}({targetaccount.ID}) adlı oyuncuyu arkadaşlıktan çıkardı!");
                }
                else
                {
                    Logger.genellog($"{account.Username}({account.ID}) {targetaccount.Username}({targetaccount.ID}) ile zaten arkadaş değil!");
                    return;
                }
            }

            // Kendi listesinden çıkar (Incremental)
            var myRemovedPacket = new FriendRemovedPacket { TargetId = targetId };
            session.Send(myRemovedPacket);

            // Karşı taraf online ise onun listesinden de çıkar (Incremental)
            if (SessionManager.IsOnline(targetaccount.ID))
            {
                Session targetSession = SessionManager.GetSession(targetaccount.ID);
                if (targetSession != null)
                {
                    var targetRemovedPacket = new FriendRemovedPacket { TargetId = account.ID };
                    targetSession.Send(targetRemovedPacket);
                }
            }
        }
            else
            {
                Logger.genellog($"{account.Username}({account.ID}) {targetaccount.Username}({targetaccount.ID}) ile zaten arkadaş değil!");
                
            }
        }
   
    }
