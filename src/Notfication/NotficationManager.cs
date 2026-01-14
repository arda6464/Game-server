public static class NotificationManager
{
    public static void Add(AccountManager.AccountData account, Notfication notification)
    {

                 if(SessionManager.IsOnline(account.AccountId))
        {
            Session session = SessionManager.GetSession(account.AccountId);
            NotficationSender.Send(session, notification);
        }
            
        account.Notfications.Add(notification);
        Logger.genellog($"{account.Username} aDLI KULLANICIYa bildirim eklendi: " + notification + "tostring hali: " + notification.ToString());
        
       
    }


    
}
