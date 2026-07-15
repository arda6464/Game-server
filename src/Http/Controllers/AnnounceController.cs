[HttpController]
public class AnnounceController : BaseController
{
    [HttpRoute("POST", "/api/announce")]
    public object SendAnnouncement()
    {
        var data = ReadFormData();
        if (data == null || !data.ContainsKey("message"))
            return Fail("Mesaj içeriği gerekli.");

        string title = data.ContainsKey("title") ? data["title"] : "SİSTEM DUYURUSU";
        string message = data["message"];
        int type = data.ContainsKey("type") && int.TryParse(data["type"], out int t) ? t : 2;

        var notificationType = type switch
        {
            0 => NotificationTypes.NotificationType.toast,
            1 => NotificationTypes.NotificationType.Inbox,
            2 => NotificationTypes.NotificationType.banner,
            _ => NotificationTypes.NotificationType.banner
        };

        var rewards = new List<RewardItem>();
        if (data.ContainsKey("rewardType") && int.TryParse(data["rewardType"], out int rewardType) && rewardType >= 0)
        {
            int rewardCount = data.ContainsKey("rewardCount") && int.TryParse(data["rewardCount"], out int rc) ? rc : 0;
            rewards.Add(new RewardItem
            {
                Type = (ItemType)rewardType,
                Count = rewardCount > 0 ? rewardCount : 1,
                DataId = 0
            });
        }

        int sentCount = 0;
        foreach (var account in AccountCache.GetAllAccounts())
        {
            var notification = new Notification
            {
                type = notificationType,
                Title = title,
                Message = message,
                Sender = "Sistem",
                Timespam = DateTime.Now,
                iconid = 2,
                IsViewed = false,
                Rewards = rewards
            };

            var session = SessionManager.GetSession(account.ID);
            var logic = session?.Logic ?? new Logic.AccountLogic(account, session);

            if (notificationType == NotificationTypes.NotificationType.toast && session != null)
            {
                NotificationSender.Send(session, notification);
            }
            else
            {
                logic.AddNotification(notification);
            }

            sentCount++;
        }

        Audit("Duyuru Yayınlandı", title, $"{sentCount} oyuncuya gönderildi. Tip: {type}");
        return Ok(new { success = true, message = $"Duyuru {sentCount} oyuncuya yayınlandı." });
    }
}
