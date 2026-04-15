using System;

[PacketHandler(MessageType.MatchMakingRequest)]
public static class MatchMakingHandler
{
    public static void Handle(Session session)
    {
        if (!DynamicConfigManager.Config.IsMatchmakingEnabled)
        {
            // Eşleştirme kapalıysa oyuncuya bildirim gönderilebilir veya istek görmezden gelinebilir.
            // session.Send(new NotificationPacket { ... });
            return;
        }

        MatchMaking.JoinQueue(session);
    }
}
