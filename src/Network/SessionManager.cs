using System.Collections.Concurrent;
using System.Net;

public static class SessionManager
{
    private static readonly ConcurrentDictionary<int, Session> activeSessions = new();
    private static readonly ConcurrentDictionary<IPEndPoint, Session> udpSessions = new(); // IPEndPoint hashcode'u güvenilir değilse string key kullanacağız, ama .NET Core'da genelde ok.

    public static Session? GetSessionByConnectionToken(int token)
    {
        return activeSessions.Values.FirstOrDefault(s => s.ConnectionToken == token);
    }

    public static Session? GetSessionByEndPoint(IPEndPoint endPoint)
    {
        udpSessions.TryGetValue(endPoint, out Session? session);
        return session;
    }


    public static void RegisterUdpSession(IPEndPoint endPoint, Session session)
    {
        // Eğer session'ın daha önce kayıtlı bir UDP adresi varsa ve bu yeni adresten farklıysa eskisini temizle
        if (session.UdpEndPoint != null && !session.UdpEndPoint.Equals(endPoint))
        {
            udpSessions.TryRemove(session.UdpEndPoint, out _);
        }

        udpSessions.TryAdd(endPoint, session);
        session.UdpEndPoint = endPoint;
        Logger.genellog($"[SessionManager] UDP Kaydı yapıldı: {session?.Account?.Username} -> {endPoint}");
    }
    public static void UnRegisterUdpSession(IPEndPoint? endPoint)
    {
        if (endPoint == null) return;
        if (udpSessions.TryRemove(endPoint, out Session? session))
        {
            session.UdpEndPoint = null;
            Logger.genellog($"[SessionManager] UDP Kaydı silindi: {session.Account?.Username} ({endPoint})");
        }
    }

    // Cmdhandler için public property
    public static ConcurrentDictionary<int, Session> GetSessions() => activeSessions;
    public static int GetCount() => activeSessions.Count;

    public static void AddSession(int id, Session session)
    {
        // Eski session varsa önce kapat (aynı hesapla tekrar giriş yapılırsa)
        if (activeSessions.TryGetValue(id, out var oldSession))
        {
            try { oldSession.Close(); } catch { }
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
                // ConcurrentDictionary üzerinde ToArray() ile güvenli snapshot alıyoruz
                var sessions = activeSessions.ToArray();

                foreach (var csession in sessions)
                {
                    var session = csession.Value;
                    var timeSinceLastAlive = DateTime.Now - session.LastAlive;

                    if (timeSinceLastAlive.TotalSeconds > 40 && session.State == Logic.PlayerState.Battle)
                    {
                        Logger.errorslog($"[PingManager] Connection timeout for {session.ID} (No packets for 40s), closing connection.");
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
