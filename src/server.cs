using System.Net;
using System.Net.Sockets;

public class GameServer
{
    private TcpListener? _listener;
    private bool _isRunning = true;

    public static UdpServer? UdpServer { get; private set; }

    public void Start(int udpPort)
    {
        try
        {
            UdpServer = new UdpServer(udpPort);
            UdpServer.Start();
            Logger.genellog($"[UDP] Server listening on {udpPort}");
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[UDP] Startup error: {ex.Message}");
        }
    }

    public void HandleConnection(TcpClient client, byte[]? initialData)
    {
        try
        {
            string clientIP = GetClientIP(client);
            Logger.genellog($"[TCP] Game client connected: {clientIP}");

            Session session = new Session(client, initialData);
            Thread clientThread = new Thread(session.Start)
            {
                IsBackground = true
            };
            clientThread.Start();
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
                catch
                {
                }
            }

            ClubCache.Stop();
            AccountCache.Stop();

            _listener?.Stop();
            UdpServer?.Stop();
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[SERVER] Shutdown error: {ex.Message}");
        }
    }
}
