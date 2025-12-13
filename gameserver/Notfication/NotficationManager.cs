public static class NotificationManager
{
    public static void Add(AccountManager.AccountData account, Notfication notification)
    {

        if (account == null)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Logger.errorslog($"[NotificationManager] {account.AccountId} hesabı bulunamadı!");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            return;
        }
            
        account.Notfications.Add(notification);
        Logger.genellog($"{account.Username} aDLI KULLANICIYa bildirim eklendi: " + notification + "tostring hali: " + notification.ToString());
        
       
    }

    public static List<Notfication> GetAll(AccountManager.AccountData account)
    {
        return account?.Notfications ?? new List<Notfication>();
    }

    public static void Clear(AccountManager.AccountData account)
    {
        if (account != null)
        {
            account.Notfications.Clear();
        }
    }
}
