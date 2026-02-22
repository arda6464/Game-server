[PacketHandler(MessageType.AllNotficationViewed)]
public static class AllNotficationViewedHandler
{
    public static void Handle(Session session)
    {

        if (session.Account == null) return;
        var acccount = session.Account;


        foreach(var notification in acccount.inboxesNotfications)
        {
            if (!notification.IsViewed)
            {
                notification.IsViewed= true;
            }
            
        }
           Console.WriteLine("Tüm bildirimler okundu");
    }
}