[HttpController]
public class DeliveryController : BaseController
{
    [HttpRoute("POST", "/api/delivery/send")]
    public object SendDelivery()
    {
        var data = ReadJsonBody<Dictionary<string, object>>();
        if (data == null || !data.ContainsKey("message") || !data.ContainsKey("rewards"))
            return Fail("Mesaj ve ödüller gerekli.");

        string title = data.ContainsKey("title") ? data["title"]?.ToString() ?? "SİSTEM" : "SİSTEM";
        string message = data["message"]?.ToString() ?? "";
        int targetId = data.ContainsKey("targetId") && int.TryParse(data["targetId"]?.ToString(), out int tid) ? tid : 0;

        var rewards = new List<RewardItem>();
        if (data["rewards"] is Newtonsoft.Json.Linq.JArray rewardArray)
        {
            foreach (var item in rewardArray)
            {
                rewards.Add(new RewardItem
                {
                    Type = (ItemType)(int.TryParse(item["Type"]?.ToString(), out int t) ? t : 0),
                    Count = int.TryParse(item["Count"]?.ToString(), out int c) ? c : 0,
                    DataId = int.TryParse(item["DataId"]?.ToString(), out int d) ? d : 0
                });
            }
        }

        var notification = new Notification
        {
            type = NotificationTypes.NotificationType.Inbox,
            Title = title,
            Message = message,
            Sender = "Sistem",
            Timespam = DateTime.Now,
            iconid = 2,
            IsViewed = false,
            Rewards = rewards
        };

        if (targetId > 0)
        {
            var account = AccountCache.Load(targetId);
            if (account == null)
                return Fail("Oyuncu bulunamadı.");

            var session = SessionManager.GetSession(targetId);
            var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
            logic.AddNotification(notification);

            Audit("Özel Paket Gönderildi", account.Username ?? targetId.ToString(), $"Ödül: {rewards.Count} adet");
        }
        else
        {
            int count = 0;
            foreach (var account in AccountCache.GetAllAccounts())
            {
                var session = SessionManager.GetSession(account.ID);
                var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
                logic.AddNotification(notification);
                count++;
            }

            Audit("Toplu Paket Gönderildi", "Tüm Oyuncular", $"{count} oyuncuya {rewards.Count} adet ödül");
        }

        return new { success = true, message = "Paket başarıyla gönderildi." };
    }
}
