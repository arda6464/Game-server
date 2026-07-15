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
        if (Context.Request.Body == null || Context.Request.Body.Length == 0) return null;
        string body = Encoding.UTF8.GetString(Context.Request.Body);

        var contentType = Context.Request.Headers.TryGetValue("Content-Type", out var ct) ? ct : "";
        if (contentType.Contains("json"))
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(body);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in body.Split('&'))
        {
            if (string.IsNullOrEmpty(pair)) continue;
            var kv = pair.Split(new[] { '=' }, 2);
            if (kv.Length == 2)
                result[Uri.UnescapeDataString(kv[0].Replace('+', ' '))] = Uri.UnescapeDataString(kv[1].Replace('+', ' '));
            else if (kv.Length == 1 && !string.IsNullOrEmpty(kv[0]))
                result[Uri.UnescapeDataString(kv[0].Replace('+', ' '))] = "";
        }
        return result;
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
