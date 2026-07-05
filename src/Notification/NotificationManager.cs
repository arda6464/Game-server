public static class NotificationManager
{
    public static void Add(AccountData account, Notification notification)
    {
        var session = SessionManager.GetSession(account.ID);
        var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
        logic.AddNotification(notification);
    }


    
}
