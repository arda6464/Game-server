using System.Text;
using Newtonsoft.Json;

public abstract class BaseController
{
    public SimpleHttpContext Context { get; set; } = null!;

    protected string AdminUser => AdminAuth.GetUsername(Context);

    protected T? ReadJsonBody<T>() where T : class
    {
        if (Context.Request.Body == null || Context.Request.Body.Length == 0) return null;
        string body = Encoding.UTF8.GetString(Context.Request.Body);
        return JsonConvert.DeserializeObject<T>(body);
    }

    protected Dictionary<string, string>? ReadFormData()
    {
        return ReadJsonBody<Dictionary<string, string>>();
    }

    protected Dictionary<string, object>? ReadRawData()
    {
        return ReadJsonBody<Dictionary<string, object>>();
    }

    protected void SetSessionCookie(string token)
    {
        Context.Response.AppendCookie("admin_session", token, "/", DateTime.Now.AddHours(24), true);
    }

    protected void ClearSessionCookie()
    {
        Context.Response.AppendCookie("admin_session", "", "/", DateTime.Now.AddDays(-1));
    }

    protected object Ok(object? data = null)
    {
        return data ?? new { success = true };
    }

    protected object Fail(string message)
    {
        return new { success = false, message };
    }

    protected object NotFound(string message = "Bulunamadı")
    {
        Context.Response.StatusCode = 404;
        return new { success = false, message };
    }

    protected void Audit(string action, string target, string details = "")
    {
        AdminAuditLogger.Log(AdminUser, action, target, details);
    }
}
