using System;
using System.Collections.Generic;

public static class NotificationPolicyManager
{
    public enum NotificationType : int
    {
        OnlineBest = 0,
        NewEvent = 1,
        Invite = 2,
        Claimed = 3
    }

    private static readonly Dictionary<NotificationType, TimeSpan> CooldownDurations = new()
    {
        { NotificationType.Invite, TimeSpan.FromHours(1) },
        { NotificationType.OnlineBest, TimeSpan.FromHours(12) },
        { NotificationType.Claimed, TimeSpan.FromHours(3) },
        { NotificationType.NewEvent, TimeSpan.FromHours(4) }
    };

    public static bool CanSendNotification(AccountManager.AccountData account, NotificationType type)
    {
        if (account == null) return false;

        lock (account.SyncLock)
        {
            // 1. Kullanıcı bu tür bildirimleri almak istiyor mu? (Ayarlar)
            bool isEnabled = type switch
            {
                NotificationType.OnlineBest => account.SendOnlineBestFriendNotification,
                NotificationType.NewEvent => account.SendNewEventNotification,
                NotificationType.Invite => account.SendInviteNotification,
                NotificationType.Claimed => account.SendClaimRewardNotification,
                _ => true
            };

            if (!isEnabled) return false;

            // 2. Cooldown kontrolü
            int typeKey = (int)type;
            if (account.NotificationCooldowns.TryGetValue(typeKey, out DateTime lastSent))
            {
                if (DateTime.Now - lastSent < CooldownDurations[type])
                {
                    return false; // Henüz süresi dolmadı
                }
            }

            return true;
        }
    }

    public static void UpdateCooldown(AccountManager.AccountData account, NotificationType type)
    {
        if (account == null) return;

        lock (account.SyncLock)
        {
            account.NotificationCooldowns[(int)type] = DateTime.Now;
        }
    }
}
