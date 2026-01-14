using System.Net;
using System.Net.Sockets;

public class GameServer
{
    private TcpListener? _listener;
    private bool _isRunning = true;

    public void Start(int port)
    {

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
            
           
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[SERVER] Shutdown hatası: {ex.Message}");
        }
    }
}