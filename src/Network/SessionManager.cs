using System.Collections.Concurrent;

public static class SessionManager
{
    private static readonly ConcurrentDictionary<int, Session> activeSessions = new();

    public static Session? GetSessionByConnectionToken(int token)
    {
        return activeSessions.Values.FirstOrDefault(s => s.ConnectionToken == token);
    }

    public static ConcurrentDictionary<int, Session> GetSessions() => activeSessions;

    public static int GetCount() => activeSessions.Count;

    public static void AddSession(int id, Session session)
    {
        if (session == null)
        {
            Logger.errorslog("[SessionManager] Hata: session null!");
            return;
        }

        if (activeSessions.TryGetValue(id, out var oldSession))
        {
            try
            {
                oldSession.Close();
            }
            catch { }
        }
        activeSessions[id] = session;
    }

    public static void RemoveSession(int id)
    {
        activeSessions.TryRemove(id, out _);
    }

    public static Session? GetSession(int id)
    {
        activeSessions.TryGetValue(id, out var session);
        return session;
    }

    public static bool IsOnline(int id)
    {
        return activeSessions.ContainsKey(id);
    }

    public static int Count() => activeSessions.Count;

    public static void PingManager(bool running)
    {
        Console.WriteLine("Ping Manager is started");
        while (running)
        {
            try
            {
                var sessions = activeSessions.ToArray();
                foreach (var csession in sessions)
                {
                    var session = csession.Value;
                    var timeSinceLastAlive = DateTime.Now - session.LastAlive;
                    if (session.State == Logic.PlayerState.Battle)
                    {
                        if (timeSinceLastAlive.TotalSeconds > 600)
                        {
                            Logger.errorslog(
                                $"[PingManager] Battle connection timeout for {session.ID} (No packets for 600s), closing connection."
                            );
                            try { session.Close(); }
                            catch
                            {
                                Logger.warnlog($"[PingManager] Failed to close timed-out battle session {session.ID}");
                            }
                        }
                    }
                    else if (timeSinceLastAlive.TotalSeconds > 40)
                    {
                        Logger.errorslog(
                            $"[PingManager] Connection timeout for {session.ID} (No packets for 40s), closing connection."
                        );
                        try { session.Close(); }
                        catch
                        {
                            Logger.warnlog($"[PingManager] Failed to close timed-out session {session.ID}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[PingManager] Hata: {ex.Message}");
            }
            Thread.Sleep(1000 * 10);
        }
    }

    public static List<Session> GetAllSessions()
    {
        return activeSessions.Values.ToList();
    }
}
