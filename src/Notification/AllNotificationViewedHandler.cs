[PacketHandler(MessageType.AllNotificationViewed)]
public static class AllNotificationViewedHandler
{
    public static void Handle(Session session)
    {

        if (session.Account == null) return;
        var acccount = session.Account;


        foreach(var notification in acccount.inboxesNotifications)
        {
            if (!notification.IsViewed)
            {
                notification.IsViewed= true;
            }
            
        }
           Console.WriteLine("Tüm bildirimler okundu");
    }
}