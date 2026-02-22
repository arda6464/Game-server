using System;

public static class NotficationSender
{
    public static void Send(Session session, Notfication notification)
    {
        if (session == null)
        {
            Logger.errorslog("[NotificationSender] Session null, gönderim yapılamadı.");
            return;
        }

        var packet = new NotificationPacket
        {
            Type = notification.type,
            Title = notification.Title,
            Message = notification.Message,
            IconId = notification.iconid,
            ButtonText = notification.ButtonText,
            Url = notification.Url,
            Sender = notification.Sender,
            IsViewed = notification.IsViewed,
            UnixTime = new DateTimeOffset(notification.Timespam.ToUniversalTime()).ToUnixTimeSeconds(),
            RewardType = (int)notification.rewardItemType,
            DonationCount = notification.DonationCount,
            IsClaimed = notification.İsclamed
        };

        session.Send(packet);

        Logger.genellog($"[NotificationSender] Bildirim gönderildi: {notification.Message}");
    }
    
   
}
