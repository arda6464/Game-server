using System;

[PacketHandler(MessageType.MatchMakingRequest)]
public static class MatchMakingHandler
{
    public static void Handle(Session session)
    {
        if (!DynamicConfigManager.Config.IsMatchmakingEnabled)
        {
            // Eşleştirme kapalıysa oyuncuya bildirim gönderilebilir veya istek görmezden gelinebilir.
            Notfication not = new Notfication
            {
                Message = "Bakımda",
                type = NotficationTypes.NotficationType.toast
            };
            NotficationSender.Send(session, not);

            return;
        }

        MatchMaking.JoinQueue(session);
    }
}
