using System;
using System.Collections.Generic;
using System.Linq;

public static class DeliveryManager
{
    /// <summary>
    /// Belirli bir oyuncuya eşya/ödül paketi gönderir.
    /// </summary>
    public static bool SendToPlayer(int playerId, string title, string message, List<RewardItem> rewards, string sender = "SİSTEM")
    {
        var account = AccountCache.Load(playerId);
        if (account == null) return false;

        var notification = new Notfication
        {
            type = NotficationTypes.NotficationType.Inbox,
            Title = title,
            Message = message,
            Sender = sender,
            Timespam = DateTime.Now,
            Rewards = rewards,
            IsClaimed = false,
            ButtonText = "AL"
        };

        lock (account.SyncLock)
        {
            notification.IndexID = account.inboxesNotfications.Count > 0 ? account.inboxesNotfications.Max(n => n.IndexID) + 1 : 1;
            account.inboxesNotfications.Add(notification);
        }
        AccountManager.SaveAccounts();

        // Online ise paketi gönder
        var session = SessionManager.GetSession(playerId);
        if (session != null)
        {
            NotficationSender.Send(session, notification);
        }

        Logger.genellog($"[DeliveryManager] {account.Username} (#{playerId}) kişisine ödül gönderildi: {title}");
        return true;
    }

    public static int SendToAll(string title, string message, List<RewardItem> rewards, string sender = "SİSTEM")
    {
        var accounts = AccountCache.GetAllAccounts();
        int count = 0;
        foreach (var account in accounts)
        {
            // Tekil save yapmayan iç mantığı kullanıyoruz (isteğe bağlı ama basitlik için direkt ekleme yapabiliriz)
            if (AddNotificationToAccount(account.ID, title, message, rewards, sender))
            {
                count++;
            }
        }
        
        // Tüm işlemler bittikten sonra BİR KEZ kaydet
        AccountManager.SaveAccounts();
        return count;
    }

    /// <summary>
    /// İç kullanım için: Hesaba bildirimi ekler ama veritabanına hemen yazmaz.
    /// </summary>
    private static bool AddNotificationToAccount(int playerId, string title, string message, List<RewardItem> rewards, string sender)
    {
        var account = AccountCache.Load(playerId);
        if (account == null) return false;

        var notification = new Notfication
        {
            type = NotficationTypes.NotficationType.Inbox,
            Title = title,
            Message = message,
            Sender = sender,
            Timespam = DateTime.Now,
            Rewards = rewards,
            IsClaimed = false,
            ButtonText = "AL"
        };

        lock (account.SyncLock)
        {
            notification.IndexID = account.inboxesNotfications.Count > 0 ? account.inboxesNotfications.Max(n => n.IndexID) + 1 : 1;
            account.inboxesNotfications.Add(notification);
        }

        // Online ise paketi gönder
        var session = SessionManager.GetSession(playerId);
        if (session != null)
        {
            NotficationSender.Send(session, notification);
        }

        return true;
    }

    /// <summary>
    /// Ödül etkisini oyuncu hesabına uygular (Drop mantığı).
    /// </summary>
    public static void ApplyReward(AccountManager.AccountData account, RewardItem reward)
    {
        switch (reward.Type)
        {
            case ItemType.Gems:
                account.Gems += reward.Count;
                break;

            case ItemType.Coins:
                account.Coins += reward.Count;
                break;

            case ItemType.BattlePass:
                account.Premium = 1;
                account.PremiumEndTime = DateTime.UtcNow.AddDays(30);
                break;

            case ItemType.Avatar:
            case ItemType.Skin:
                if (!account.OwnedItems.Contains(reward.DataId))
                    account.OwnedItems.Add(reward.DataId);
                account.Avatarid = reward.DataId;
                break;

            case ItemType.Character:
                if (!account.OwnedItems.Contains(reward.DataId))
                    account.OwnedItems.Add(reward.DataId);
                break;

            case ItemType.PowerPoints:
                // TODO: Karakter bazlı puan ekleme
                break;

            case ItemType.XPBoost:
                // TODO: XP Boost entegrasyonu
                break;

            case ItemType.Emote:
                // TODO: Emote/Pin kilit açma
                break;
        }

        Logger.genellog($"[DeliveryManager] Ödül uygulandı: {reward.Type} (Data: {reward.DataId}, Count: {reward.Count}) -> {account.Username}");
    }
}
