using System;

[PacketHandler(MessageType.MatchMakingRequest)]
public static class MatchMakingHandler
{
    public static void Handle(Session session)
    {
        if (!DynamicConfigManager.Config.IsMatchmakingEnabled)
        {
            // Eşleştirme kapalıysa oyuncuya bildirim gönderilebilir veya istek görmezden gelinebilir.
            Notification not = new Notification
            {
                Message = "Bakımda",
                type = NotificationTypes.NotificationType.toast
            };
            NotificationSender.Send(session, not);

            return;
        }

        MatchMaking.JoinQueue(session);
    }
}
