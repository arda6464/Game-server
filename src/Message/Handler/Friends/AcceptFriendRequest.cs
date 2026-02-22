[PacketHandler(MessageType.AcceptFriendRequest)]
public static class FriendRequestAccept
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer byteBuffer = new ByteBuffer();
        byteBuffer.WriteBytes(message, true);
        int _ = byteBuffer.ReadShort();

        var requestPacket = new FriendRequestAcceptPacket();
        requestPacket.Deserialize(byteBuffer);

        string targetId = requestPacket.TargetId;
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
                var request = account.Requests.Find(r => r.Id == targetId);
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
                Username = target.Username,
                AvatarId = target.Avatarid,
                Id = target.AccountId,
                NameColorID = target.Namecolorid,
                IsBestFriend = false,
                Trophy = target.Trophy
            };

            FriendInfo friendForTarget = new FriendInfo()
            {
                Username = account.Username,
                AvatarId = account.Avatarid,
                Id = account.AccountId,
                NameColorID = account.Namecolorid,
                IsBestFriend = false,
                Trophy = account.Trophy
            };

            // Listelere ekle (Thread-safe)
            lock (account.SyncLock)
            {
                if (!account.Friends.Any(f => f.Id == targetId))
                {
                    account.Friends.Add(friendForAccount);
                }
            }

            lock (target.SyncLock)
            {
                if (!target.Friends.Any(f => f.Id == account.AccountId))
                {
                    target.Friends.Add(friendForTarget);
                }
            }

            // Görev İlerlemesi - Arkadaş Ekleme
            QuestManager.CheckQuestProgress(account, Quest.MissionType.AddFriend);
            QuestManager.CheckQuestProgress(target, Quest.MissionType.AddFriend);

            Console.WriteLine($"{account.Username}({account.AccountId}) ile {target.Username}({target.AccountId}) arkadaş oldu.");

            // Kendi listesine yeni arkadaşı ekle (Incremental)
            var myFriendAddedPacket = new FriendAddedPacket { Friend = friendForAccount };
            session.Send(myFriendAddedPacket);

            // Karşı taraf online ise ona da yeni arkadaşı ekle (Incremental)
            if (SessionManager.IsOnline(targetId))
            {
                Session targetSession = SessionManager.GetSession(targetId);
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