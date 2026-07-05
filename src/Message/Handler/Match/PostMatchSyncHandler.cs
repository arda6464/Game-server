using System;
using System.Linq;
using Logic;

[PacketHandler(MessageType.PostMatchSyncRequest)]
public static class PostMatchSyncHandler
{
    public static void Handle(Session session)
    {
        if (session?.Account == null)
        {
            Logger.errorslog("[PostMatchSync] Account bulunamadı, sync atlandı.");
            return;
        }

        var account = session.Account;
        SeasonManager.EnsureAccountSeasonState(account);
        bool wasAlreadyLobby = session.State == PlayerState.Lobby;

        if (session.BattleId > 0)
        {
            var battle = ArenaManager.GetBattle(session.BattleId);
            if (battle != null && battle.State == BattleState.Active)
            {
                battle.RemovePlayer(session.ID);
            }

            session.BattleId = 0;
            if (session.PlayerData != null)
                session.PlayerData.BattleId = 0;
        }

        SessionManager.UnRegisterUdpSession(session.UdpEndPoint);

        if (!wasAlreadyLobby)
        {
            if (session.State != PlayerState.Lobby)
            {
                session.ChangeState(PlayerState.Lobby);
            }
        }
        else
        {
            session.Logic?.HomeVisited();
        }

        var response = new HomeStateSyncResponsePacket
        {
            Account = account,
            Club = account.Clubid > 0 ? ClubManager.LoadClub(account.Clubid) : null,
            RandomClubs = ClubManager.RandomList(10),
            OnlinePlayers = OnlinePlayerManager.BuildSnapshotForViewer(account),
            Quests = account.Quests.ToList(),
            NextQuestRefreshTime = QuestManager.GetNextQuestRefreshTime(),
            NextSeasonalQuestRefreshTime = QuestManager.GetNextSeasonalQuestRefreshTime()
        };

        session.Send(response);

        // Home ekranındaki dinamik kartlar için mevcut paketleri de yenileyelim.
        GetEvents.Handle(session, Array.Empty<byte>());
        ShopItemsHandler.Handle(session);

        lock (account.SyncLock)
        {
            foreach (var inboxNotification in account.inboxesNotifications)
            {
                NotificationSender.Send(session, inboxNotification);
            }

            foreach (var notification in account.Notifications)
            {
                if (!notification.IsViewed)
                {
                    NotificationSender.Send(session, notification);
                    notification.IsViewed = true;
                }
            }
        }

        AccountManager.SaveAccounts();
        Logger.genellog($"[PostMatchSync] Home sync gönderildi: {account.Username} ({account.ID})");
    }
}
