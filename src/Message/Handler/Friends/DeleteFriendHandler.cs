[PacketHandler(MessageType.DeleteFriendRequest)]
public static class DeleteFriendHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        int _ = read.ReadShort();
        
        var request = new DeleteFriendRequestPacket();
        request.Deserialize(read);
        
        string acccountId = request.TargetId;
         read.Dispose();
        if (session.Account == null) return;
        AccountManager.AccountData targetaccount = AccountCache.Load(acccountId);
        AccountManager.AccountData account = session.Account;
        if (account != null && targetaccount != null)
        {
            lock (account.SyncLock)
            lock (targetaccount.SyncLock)
            {
                var friends = account.Friends.Find(f => f.Id == acccountId);
                var targetfriends = targetaccount.Friends.Find(f => f.Id == session.AccountId);
                if (friends != null && targetfriends != null)
                {
                    account.Friends.Remove(friends);
                    targetaccount.Friends.Remove(targetfriends);
                    Logger.genellog($"{account.Username}({account.AccountId}) adlı oyuncu {targetaccount.Username}({targetaccount.AccountId}) adlı oyuncuyu arkadaşlıktan çıkardı!");
                }
                else
                {
                    Logger.genellog($"{account.Username}({account.AccountId}) {targetaccount.Username}({targetaccount.AccountId}) ile zaten arkadaş değil!");
                    return;
                }
            }

            // Kendi listesinden çıkar (Incremental)
            var myRemovedPacket = new FriendRemovedPacket { TargetId = acccountId };
            session.Send(myRemovedPacket);

            // Karşı taraf online ise onun listesinden de çıkar (Incremental)
            if (SessionManager.IsOnline(targetaccount.AccountId))
            {
                Session targetSession = SessionManager.GetSession(targetaccount.AccountId);
                if (targetSession != null)
                {
                    var targetRemovedPacket = new FriendRemovedPacket { TargetId = session.AccountId };
                    targetSession.Send(targetRemovedPacket);
                }
            }
        }
            else
            {
                Logger.genellog($"{account.Username}({account.AccountId}) {targetaccount.Username}({targetaccount.AccountId}) ile zaten arkadaş değil!");
                
            }
        }
   
    }
