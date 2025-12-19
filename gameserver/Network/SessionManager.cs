public static class SessionManager
{
    private static readonly Dictionary<string, Session> activeSessions = new();
    
    // Cmdhandler için public property
    public static Dictionary<string, Session> GetSessions() => activeSessions;
    public static int GetCount() => activeSessions.Count;
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
    public static int Count() => activeSessions.Count();

    public static void PingManager(bool running)
    {
       
        Console.WriteLine("Ping Manager is started");
        while(running)
        {
            var now = DateTime.Now;
            foreach (var csession in activeSessions)
            {
                // Check ping timeout (30 saniye ping alınmazsa disconnect)
                var session = csession.Value;
                var timeSinceLastPing = DateTime.Now - session.LastPingSent;

                if (timeSinceLastPing.TotalSeconds > 20)
                {
                    Logger.errorslog($"[PingManager] Ping timeout for {session.AccountId}, closing connection.");
                    session.Close();
                }
                
            }
            Thread.Sleep(1000 * 10);
        }
    }
}
