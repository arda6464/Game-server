public static class SessionManager
{
    private static readonly Dictionary<string, Session> activeSessions = new();

    public static void AddSession(string accountId, Session session)
    {
        if (!activeSessions.ContainsKey(accountId))
            activeSessions.Add(accountId, session);
    }

    public static void RemoveSession(string accountId)
    {
        activeSessions.Remove(accountId);
    }

    public static Session? GetSession(string accountId)
    {
        activeSessions.TryGetValue(accountId, out var session);
        return session;
    }

    public static bool IsOnline(string accountId)
    {
        return activeSessions.ContainsKey(accountId);
    }
}
