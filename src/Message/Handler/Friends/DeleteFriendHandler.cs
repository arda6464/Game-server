[PacketHandler(MessageType.DeleteFriendRequest)]
public class DeleteFriendHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var request = data.DeserializePacket<DeleteFriendRequestPacket>();

        int targetId = request.TargetId;
        if (session.Account == null)
            return;
        AccountData targetaccount = AccountCache.Load(targetId);
        AccountData account = session.Account;
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
                        Logger.genellog(
                            $"{account.Username}({account.ID}) adlı oyuncu {targetaccount.Username}({targetaccount.ID}) adlı oyuncuyu arkadaşlıktan çıkardı!"
                        );
                    }
                    else
                    {
                        Logger.genellog(
                            $"{account.Username}({account.ID}) {targetaccount.Username}({targetaccount.ID}) ile zaten arkadaş değil!"
                        );
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
            Logger.genellog(
                $"{account.Username}({account.ID}) {targetaccount.Username}({targetaccount.ID}) ile zaten arkadaş değil!"
            );
        }
    }
}
