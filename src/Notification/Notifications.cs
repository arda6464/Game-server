public class Notification
{
    public int IndexID { get; set; }
    public NotificationTypes.NotificationType type { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string ButtonText;
    public bool IsViewed = false;
    public string Url;
    public int iconid = 2;
    public string Sender;
    public DateTime Timespam = DateTime.Now;
    public List<RewardItem> Rewards { get; set; } = new List<RewardItem>();
    public bool IsClaimed { get; set; }

    public Notification() { }
}

public class NotificationTypes
{
    public enum NotificationType : byte
    {
        toast,
        banner,
        Inbox,
        Push,
    }
}
