[PacketHandler(MessageType.AcceptFriendRequest)]
public static class FriendRequestAccept
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer byteBuffer = new ByteBuffer();
        byteBuffer.WriteBytes(message, true);

        var requestPacket = new FriendRequestAcceptPacket();
        requestPacket.Deserialize(byteBuffer);

        int targetId = requestPacket.TargetId;
        byteBuffer.Dispose();

        
      


        if (session.Account == null) return;
        AccountManager.AccountData account = session.Account; // isteği kabul eden kişi
        AccountManager.AccountData target = AccountCache.Load(targetId); // isteği kabul edilen kişi
        if (target == null) 
        {
            Logger.errorslog($"[Friend manager] {targetId}'li hesap bulunamadı");
            return;
        }
        try
        {
            lock (account.SyncLock)
            {
                var request = account.Requests.Find(r => r.ID == targetId);
                if (request != null)
                {
                    account.Requests.Remove(request);
                }
                else
                {
                    Logger.errorslog($"[Friend manager] {targetId} için gelen bir istek bulunamadı.");
                    return;
                }
            }

            // Arkadaşlık bilgilerini hazırla
            FriendInfo friendForAccount = new FriendInfo()
            {
                ID = target.ID,
                Username = target.Username,
                AvatarId = target.Avatarid,
                NameColorID = target.Namecolorid,
                IsBestFriend = false,
                Trophy = target.Trophy
            };

            FriendInfo friendForTarget = new FriendInfo
            {
                ID = account.ID,
                Username = account.Username,
                AvatarId = account.Avatarid,
                NameColorID = account.Namecolorid,
                IsBestFriend = false,
                Trophy = account.Trophy
            };

            // Listelere ekle (Thread-safe)
            lock (account.SyncLock)
            {
                if (!account.Friends.Any(f => f.ID == targetId))
                {
                    account.Friends.Add(friendForAccount);
                }
            }

            lock (target.SyncLock)
            {
                if (!target.Friends.Any(f => f.ID == account.ID))
                {
                    target.Friends.Add(friendForTarget);
                }
            }

            // Görev İlerlemesi - Arkadaş Ekleme
            QuestManager.CheckQuestProgress(account, Quest.MissionType.AddFriend);
            QuestManager.CheckQuestProgress(target, Quest.MissionType.AddFriend);

            Console.WriteLine($"{account.Username}({account.ID}) ile {target.Username}({target.ID}) arkadaş oldu.");

            // Kendi listesine yeni arkadaşı ekle (Incremental)
            var myFriendAddedPacket = new FriendAddedPacket { Friend = friendForAccount };
            session.Send(myFriendAddedPacket);

            // Karşı taraf online ise ona da yeni arkadaşı ekle (Incremental)
            if (SessionManager.IsOnline(target.ID))
            {
                Session targetSession = SessionManager.GetSession(target.ID);
                if (targetSession != null)
                {
                    var targetFriendAddedPacket = new FriendAddedPacket { Friend = friendForTarget };
                    targetSession.Send(targetFriendAddedPacket);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.errorslog("AcceptFriendRequest hata: " + ex.Message + "\n" + ex.ToString());
        }


    }
}
