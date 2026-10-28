using System.Net;
using System.Net.Sockets;

public class GameServer
{
    private bool _isRunning = true;

    public static BattleServer? BattleServer { get; private set; }

    public void Start(int udpPort)
    {
        try
        {
            int battlePort = udpPort + 100;
            BattleServer = new BattleServer(battlePort);
            Logger.genellog($"[BATTLE] LES server listening on {battlePort}");
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[BATTLE] Startup error: {ex.Message}");
        }
    }

    public void HandleConnection(TcpClient client, byte[]? initialData)
    {
        try
        {
            string clientIP = GetClientIP(client);
            Logger.genellog($"[TCP] Game client connected: {clientIP}");

            Session session = new Session(client, initialData);
            _ = session.StartAsync();
        }
        catch (Exception ex)
        {
            Logger.errorslog($"HandleConnection error: {ex.Message}");
        }
    }

    private string GetClientIP(TcpClient client)
    {
        try
        {
            if (client?.Client?.RemoteEndPoint is IPEndPoint remoteEndPoint)
            {
                return remoteEndPoint.Address.ToString();
            }
        }
        catch (Exception ex)
        {
            Logger.errorslog($"IP lookup error: {ex.Message}");
        }

        return "Unknown IP";
    }

    public void Stop()
    {
        _isRunning = false;
        Logger.bootlog("[SERVER] Shutdown starting...");

        try
        {
            foreach (var session in SessionManager.GetSessions())
            {
                try
                {
                    session.Value.Close();
                }
                catch { }
            }

            ClubCache.Stop();
            AccountCache.Stop();

            BattleServer?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[SERVER] Shutdown error: {ex.Message}");
        }
    }
}
