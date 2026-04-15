using System.Net;
using System.Net.Sockets;

public class GameServer
{
    private TcpListener? _listener;
    private bool _isRunning = true;

    public static UdpServer? UdpServer { get; private set; }

    public void Start(int udpPort)
    {
        // UDP Sunucusunu başlat (Dış porttan direkt dinle)
        try
        {
            UdpServer = new UdpServer(udpPort);
            UdpServer.Start();
            Logger.genellog($"[UDP] Sunucu dinleniyor: {udpPort}");
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[UDP] Başlatma hatası: {ex.Message}");
        }
    }

    public void HandleConnection(TcpClient client, byte[]? initialData)
    {
        try
        {
            string clientIP = GetClientIP(client);
            Console.WriteLine($"Yeni oyun client'ı bağlandı! IP: {clientIP}");

            Session session = new Session(client, initialData);
            Thread clientThread = new Thread(session.Start);
            clientThread.Start();
        }
        catch (Exception ex)
        {
            Logger.errorslog($"HandleConnection hatası: {ex.Message}");
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
            Logger.errorslog($"IP alma hatası: {ex.Message}");
        }
        return "Bilinmeyen IP";
    }


    public void Stop()
    {
        _isRunning = false;
        Logger.genellog("[SERVER] Shutdown başlatılıyor...");

        try
        {
            // Tüm bağlantıları kapat

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

            // TcpListener'ı kapat
            _listener?.Stop();
            UdpServer?.Stop();


        }
        catch (Exception ex)
        {
            Logger.errorslog($"[SERVER] Shutdown hatası: {ex.Message}");
        }
    }
}