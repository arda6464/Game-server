using System;
using System.Collections.Concurrent;

public static class AdminAuth
{
    private static readonly ConcurrentDictionary<string, (string Username, DateTime Expiry)> _sessions = new();

    public static string CreateSession(string username)
    {
        string token = Guid.NewGuid().ToString();
        _sessions[token] = (username, DateTime.Now.AddHours(24));
        return token;
    }

    public static bool TryGetSession(string token, out string username)
    {
        username = "";
        if (string.IsNullOrEmpty(token)) return false;

        if (_sessions.TryGetValue(token, out var session))
        {
            if (DateTime.Now < session.Expiry)
            {
                username = session.Username;
                return true;
            }
            _sessions.TryRemove(token, out _);
        }
        return false;
    }

    public static void RevokeSession(string token)
    {
        if (!string.IsNullOrEmpty(token))
            _sessions.TryRemove(token, out _);
    }

    public static string GetToken(SimpleHttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out string? authHeader))
        {
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return authHeader.Substring(7).Trim();
        }
        if (context.Request.Cookies.TryGetValue("admin_session", out string? cookieToken))
            return cookieToken;
        return "";
    }

    public static bool IsAuthorized(SimpleHttpContext context)
    {
        string token = GetToken(context);
        return TryGetSession(token, out _);
    }

    public static string GetUsername(SimpleHttpContext context)
    {
        string token = GetToken(context);
        return TryGetSession(token, out var username) ? username : "Bilinmiyor";
    }
}
