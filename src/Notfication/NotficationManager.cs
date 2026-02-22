public static class NotificationManager
{
    public static void Add(AccountManager.AccountData account, Notfication notification)
    {
        if(notification.type == NotficationTypes.NotficationType.Push)
        {
            Console.WriteLine("Account token: " + account.FBNToken);
            AndroidNotficationManager.SendNotification(notification.Title, notification.Message, account.FBNToken);
            return; // kaydedilmesini istemiyoruz
        }

                 if(SessionManager.IsOnline(account.AccountId) && notification.type != NotficationTypes.NotficationType.Push)
        {
            Session session = SessionManager.GetSession(account.AccountId);
            NotficationSender.Send(session, notification);
        }
        account.Notfications.Add(notification);
        Logger.genellog($"{account.Username} ADLI KULLANICIYA bildirim eklendi: " + notification);
    }


    
}
