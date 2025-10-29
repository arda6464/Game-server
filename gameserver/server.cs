using System.Net;
using System.Net.Sockets;

public class GameServer
{
    private TcpListener? _listener;
    private bool _isRunning = true;

    public void Start()
    {
       
        _listener = new TcpListener(IPAddress.Any, 5000);
        _listener.Start();
        Logger.genellog("Sunucu port 5000'de dinleniyor...");
        
        while (_isRunning)
        {
            try
            {
                TcpClient client = _listener.AcceptTcpClient();
                Console.WriteLine("Yeni bir client bağlandı!");
             
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