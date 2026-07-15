using System;

[PacketHandler(MessageType.MatchMakingRequest)]
public class MatchMakingHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        if (!DynamicConfigManager.Config.IsMatchmakingEnabled)
        {
            // Eşleştirme kapalıysa oyuncuya bildirim gönderilebilir veya istek görmezden gelinebilir.
            Notification not = new Notification
            {
                Message = "Bakımda",
                type = NotificationTypes.NotificationType.toast,
            };
            NotificationSender.Send(session, not);

            return;
        }

        MatchMaking.JoinQueue(session);
    }
}
