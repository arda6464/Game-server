public static class AllNotficationViewedHandler
{
    public static void Handle(Session session)
    {

        var acccount = AccountCache.Load(session.AccountId);


        foreach(var notification in acccount.inboxesNotfications)
        {
            if (!notification.İsViewed)
            {
                notification.İsViewed = true;
            }
            Console.WriteLine("Tüm bildirimler okundu");
        }

    }
}