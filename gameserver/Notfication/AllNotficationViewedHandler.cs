public static class AllNotficationViewedHandler
{
    public static void Handle(Session session)
    {

        var acccount = AccountCache.Load(session.AccountId);


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