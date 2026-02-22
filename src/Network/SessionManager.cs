using System.Collections.Concurrent;
using System.Net;

public static class SessionManager
{
    private static readonly ConcurrentDictionary<string, Session> activeSessions = new();
    private static readonly ConcurrentDictionary<IPEndPoint, Session> udpSessions = new(); // IPEndPoint hashcode'u güvenilir değilse string key kullanacağız, ama .NET Core'da genelde ok.
    
    public static Session? GetSessionByConnectionToken(int token)
    {
        return activeSessions.Values.FirstOrDefault(s => s.ConnectionToken == token);
    }


    public static void RegisterUdpSession(IPEndPoint endPoint, Session session)
    {
        udpSessions.TryAdd(endPoint, session);
        session.UdpEndPoint = endPoint;
        Logger.genellog($"[SessionManager] UDP Kaydı yapıldı: {session.Account.Username} -> {endPoint}");
    }
    
    // Cmdhandler için public property
    public static ConcurrentDictionary<string, Session> GetSessions() => activeSessions;
    public static int GetCount() => activeSessions.Count;

    public static void AddSession(string accountId, Session session)
    {
        // Eski session varsa önce kapat (aynı hesapla tekrar giriş yapılırsa)
        if (activeSessions.TryGetValue(accountId, out var oldSession))
        {
            try { oldSession.Close(); } catch { }
        }
        activeSessions[accountId] = session;
    }

    public static void RemoveSession(string accountId)
    {
        activeSessions.TryRemove(accountId, out _);
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

    public static int Count() => activeSessions.Count;

    public static void PingManager(bool running)
    {
        Console.WriteLine("Ping Manager is started");
        while (running)
        {
            try
            {
                // ConcurrentDictionary üzerinde ToArray() ile güvenli snapshot alıyoruz
                var sessions = activeSessions.ToArray();
                
                foreach (var csession in sessions)
                {
                    var session = csession.Value;
                    var timeSinceLastAlive = DateTime.Now - session.LastAlive;

                    if (timeSinceLastAlive.TotalSeconds > 40)
                    {
                        Logger.errorslog($"[PingManager] Connection timeout for {session.AccountId} (No packets for 40s), closing connection.");
                        try { session.Close(); } catch { }
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
