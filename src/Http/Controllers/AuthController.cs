using Newtonsoft.Json;

[HttpController]
public class AuthController : BaseController
{
    [HttpRoute("POST", "/api/auth/login", RequiresAuth = false)]
    public object Login(SimpleHttpContext ctx)
    {
        var data = ctx.Request.Headers.TryGetValue("Content-Type", out var ct) && ct.Contains("json")
            ? ReadJsonBody<Dictionary<string, string>>()
            : ReadFormData();

        if (data == null || !data.ContainsKey("username") || !data.ContainsKey("password"))
            return Fail("Kullanıcı adı ve şifre gerekli.");

        var account = AdminAccountManager.Authenticate(data["username"], data["password"], ctx.Request.RemoteEndPoint);
        if (account == null)
            return Fail("Geçersiz kullanıcı adı veya şifre.");

        string token = AdminAuth.CreateSession(account.Username);
        ctx.Response.AppendCookie("admin_session", token, "/", DateTime.Now.AddHours(24), true);
        return new { success = true, token = token, username = account.Username, role = account.Role };
    }

    [HttpRoute("POST", "/api/auth/logout")]
    public object Logout(SimpleHttpContext ctx)
    {
        string token = AdminAuth.GetToken(ctx);
        AdminAuth.RevokeSession(token);
        ctx.Response.AppendCookie("admin_session", "", "/", DateTime.Now.AddDays(-1));
        return Ok();
    }

    [HttpRoute("GET", "/api/auth/check", RequiresAuth = false)]
    public object Check(SimpleHttpContext ctx)
    {
        return new { success = true, authorized = AdminAuth.IsAuthorized(ctx) };
    }
}
