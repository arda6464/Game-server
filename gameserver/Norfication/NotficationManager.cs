public static class NotificationManager
{
    public static void Add(AccountManager.AccountData account, Notification notification)
    {

        if (account == null)
        {
            Logger.errorslog($"[NotificationManager] {account.AccountId} hesabı bulunamadı!");
            return;
        }

        account.Notifications.Add(notification);
        Logger.genellog($"{account.Username} aDLI KULLANICIYa bildirim eklendi: " + notification + "tostring hali: " + notification.ToString());
        
       
    }

    public static List<Notification> GetAll(AccountManager.AccountData account)
    {
        return account?.Notifications ?? new List<Notification>();
    }

    public static void Clear(AccountManager.AccountData account)
    {
        if (account != null)
        {
            account.Notifications.Clear();
        }
    }
}
