[HttpController]
public class BroadcastController : BaseController
{
    [HttpRoute("POST", "/api/broadcast/alert")]
    public object SendAlert()
    {
        var data = ReadFormData();
        if (data == null || !data.ContainsKey("message"))
            return Fail("Mesaj gerekli.");

        string title = data.ContainsKey("title") ? data["title"] : "Sistem Duyurusu";
        string message = data["message"];

        int count = 0;
        foreach (var account in AccountCache.GetAllAccounts())
        {
            var notification = new Notification
            {
                type = NotificationTypes.NotificationType.toast,
                Title = title,
                Message = message,
                Sender = "Sistem",
                Timespam = DateTime.Now,
                iconid = 1,
                IsViewed = false
            };

            var session = SessionManager.GetSession(account.ID);
            if (session != null)
            {
                NotificationSender.Send(session, notification);
                count++;
            }
        }

        Audit("Acil Duyuru", "Tüm Çevrimiçi Oyuncular", $"{count} oyuncuya gönderildi: {message}");
        return Ok(new { success = true, message = $"Duyuru {count} oyuncuya iletildi." });
    }
}
