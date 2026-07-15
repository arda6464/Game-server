using System.Linq;

[HttpController]
public class AdminController : BaseController
{
    [HttpRoute("GET", "/api/admin/accounts")]
    public object GetAccounts()
    {
        var accounts = AdminAccountManager.GetAccounts();
        return accounts.Select(a => new
        {
            username = a.Username,
            role = a.Role,
            createdAt = a.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
            lastLogin = a.LastLogin > DateTime.MinValue ? a.LastLogin.ToString("dd.MM.yyyy HH:mm") : "Hiç giriş yapmadı",
            lastIp = a.LastIp ?? "N/A"
        }).ToList();
    }

    [HttpRoute("GET", "/api/admin/logs")]
    public object GetLogs()
    {
        var logs = AdminAuditLogger.GetLogs();
        return logs.Select(l => new
        {
            time = l.Timestamp.ToString("dd.MM.yyyy HH:mm"),
            admin = l.AdminUsername,
            action = l.Action,
            target = l.Target,
            details = l.Details
        }).ToList();
    }

    [HttpRoute("POST", "/api/admin/create")]
    public object CreateAccount()
    {
        var data = ReadFormData();
        if (data == null || !data.ContainsKey("username") || !data.ContainsKey("password"))
            return Fail("Kullanıcı adı ve parola gerekli.");

        string username = data["username"].Trim();
        string password = data["password"];
        string role = data.ContainsKey("role") ? data["role"] : "Admin";

        bool created = AdminAccountManager.CreateAccount(username, password, role);
        if (!created)
            return Fail("Bu kullanıcı adı zaten mevcut.");

        Audit("Admin Hesabı Oluşturuldu", username, $"Rol: {role}");
        return new { success = true, message = "Admin hesabı oluşturuldu." };
    }

    [HttpRoute("POST", "/api/admin/delete")]
    public object DeleteAccount()
    {
        var data = ReadFormData();
        if (data == null || !data.ContainsKey("username"))
            return Fail("Kullanıcı adı gerekli.");

        string username = data["username"].Trim();
        bool deleted = AdminAccountManager.DeleteAccount(username);
        if (!deleted)
            return Fail("Admin silinemedi. (Owner silinemez veya kullanıcı bulunamadı)");

        Audit("Admin Hesabı Silindi", username, "Hesap sistemden kaldırıldı.");
        return new { success = true, message = "Admin hesabı silindi." };
    }
}
