using System.Net;
using System.Net.Sockets;

public class GameServer
{
    private TcpListener? _listener;
    private bool _isRunning = true;

    public static UdpServer? UdpServer { get; private set; }

    public void Start(int port)
    {
        // UDP Sunucusunu başlat (TCP portu + 1 veya aynı port kullanılabilir ama genelde farklı olması iyidir, şimdilik +1 diyelim veya aynısı)
        // Eğer TCP ve UDP aynı portta çalışacaksa (örn 7777), o zaman aynı portu verelim.
        // Genelde oyunlarda TCP ve UDP aynı port numarasını kullanır (protokol farklı olduğu için çakışmaz).
        try
        {
            UdpServer = new UdpServer(port);
            UdpServer.Start();
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[UDP] Başlatma hatası: {ex.Message}");
        }

        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        Logger.genellog($"Sunucu  {port} portunda dinleniyor...");

        while (_isRunning)
        {
            try
            {
                TcpClient client = _listener.AcceptTcpClient();
                string clientIP = GetClientIP(client);
                Console.WriteLine($"Yeni client bağlandı! IP: {clientIP}");
                    
                Session session = new Session(client);
                Thread clientThread = new Thread(session.Start);
                clientThread.Start();
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Logger.errorslog($"AcceptTcpClient hatası: {ex.Message}");
                }
            }
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