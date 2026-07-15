using System.Linq;



public static class FriendsManager
{
    public static void SendRequest(AccountData from, AccountData to)
    {
        lock (from.SyncLock)
        {
            if (from.Friends.Any(f => f.ID == to.ID))
            {
                Logger.errorslog($"{from.Username} zaten {to.Username}'nin arkadaşı");
                return;
            }
        }

        lock (to.SyncLock)
        {
            if (to.Requests.Any(r => r.ID == from.ID))
            {
                Logger.errorslog($"{from.Username} zaten {to.Username}'ye istek göndermiş");
                return;
            }

            if (from.ID == to.ID)
            {
                Logger.errorslog($"{from.Username} kendine istek atıyor");
                return;
            }

            var info = new Friend
            {
                ID = from.ID,
                Username = from.Username,
                AvatarId = from.Avatarid,
                NameColorID = from.Namecolorid,
                Trophy = from.Trophy
            };

            to.Requests.Add(info);

            if (SessionManager.IsOnline(to.ID))
            {
                var targetSession = SessionManager.GetSession(to.ID);
                targetSession?.Send(new FriendRequestAddedPacket { Request = info });
            }
        }

        Logger.genellog($"{from.Username}({from.ID}) → {to.Username}({to.ID}) 'ye istek attı");
    }

    public static void AcceptRequest(AccountData account, AccountData target, Session session)
    {
        lock (account.SyncLock)
        {
            var request = account.Requests.Find(r => r.ID == target.ID);
            if (request != null)
                account.Requests.Remove(request);
            else
            {
                Logger.errorslog($"[FriendsManager] {target.ID} için gelen bir istek bulunamadı.");
                return;
            }
        }

        var friendForAccount = new Friend
        {
            ID = target.ID, Username = target.Username,
            AvatarId = target.Avatarid, NameColorID = target.Namecolorid,
            IsBestFriend = false, Trophy = target.Trophy
        };

        var friendForTarget = new Friend
        {
            ID = account.ID, Username = account.Username,
            AvatarId = account.Avatarid, NameColorID = account.Namecolorid,
            IsBestFriend = false, Trophy = account.Trophy
        };

        lock (account.SyncLock)
        {
            if (!account.Friends.Any(f => f.ID == target.ID))
                account.Friends.Add(friendForAccount);
        }

        lock (target.SyncLock)
        {
            if (!target.Friends.Any(f => f.ID == account.ID))
                target.Friends.Add(friendForTarget);
        }

        QuestManager.CheckQuestProgress(account, Quest.MissionType.AddFriend);
        QuestManager.CheckQuestProgress(target, Quest.MissionType.AddFriend);

        Logger.genellog($"{account.Username}({account.ID}) ile {target.Username}({target.ID}) arkadaş oldu.");

        session.Send(new FriendAddedPacket { Friend = friendForAccount });

        if (SessionManager.IsOnline(target.ID))
        {
            var targetSession = SessionManager.GetSession(target.ID);
            targetSession?.Send(new FriendAddedPacket { Friend = friendForTarget });
        }
    }

    public static bool DeclineRequest(AccountData account, AccountData target)
    {
        lock (account.SyncLock)
        {
            var req = account.Requests.Find(r => r.ID == target.ID);
            if (req != null)
            {
                account.Requests.Remove(req);
                Logger.genellog($"{account.Username}({account.ID}) {target.Username}({target.ID}) isteğini reddetti");
                return true;
            }
        }
        Logger.errorslog($"[FriendsManager] {target.ID} için reddedilecek istek bulunamadı.");
        return false;
    }

    public static void SetBestFriend(AccountData account, int targetId)
    {
        var friend = account.Friends.Find(f => f.ID == targetId);
        if (friend == null) return;
        friend.IsBestFriend = !friend.IsBestFriend;
        Logger.genellog($"{account.Username} ({account.ID}) best friend değişikliği: target={targetId} bestFriend={friend.IsBestFriend}");
    }
}
