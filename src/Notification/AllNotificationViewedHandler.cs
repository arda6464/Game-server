[PacketHandler(MessageType.AllNotificationViewed)]
public class AllNotificationViewedHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        if (session.Account == null)
            return;
        var acccount = session.Account;

        foreach (var notification in acccount.inboxesNotifications)
        {
            if (!notification.IsViewed)
            {
                notification.IsViewed = true;
            }
        }
        Console.WriteLine("Tüm bildirimler okundu");
    }
}
