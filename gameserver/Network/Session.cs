using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

public class Session
{
    private TcpClient client;
    private NetworkStream stream;
    public Player? PlayerData { get; set; }
    public string AccountId { get; set; }
    public DateTime LastPingSent { get; set; }
    public int LastPing { get; set; }
    public string DeviceID { get; set; }
    public string IP { get; set; }
    public int TeamID = 0;
   
    
    private bool _isClosed = false; // ✅ YENİ: Çift çağrıyı önlemek için flag
    private object _closeLock = new object(); // ✅ YENİ: Thread safety için lock

    public Session(TcpClient c)
    {
        client = c;
        this.stream = client.GetStream();
        IP = GetClientIP();
    }
    
    public void Start()
    {
        byte[] buffer = new byte[4096];

        try
        {
            while (true)
            {
                int bytesRead = 0;
                try
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("[Session] Stream kapalı, client çıkış yaptı.");
                    break;
                }
                catch (IOException)
                {
                    Console.WriteLine("[Session] Client bağlantısı kesildi.");
                    break;
                }

                if (bytesRead <= 0)
                {
                    Console.WriteLine("[Session] Client bağlantıyı kapattı.");
                    break;
                }

                MessageManager.HandleMessage(this, buffer);
            }
        }
        finally
        {
            Close();
        }
    }

    private string GetClientIP()
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
    
    public void Send(byte[] buffer)
    {
        if (!client.Connected)
        {
            throw new InvalidOperationException("Bağlantı kapalı");
        }
        try
        {
            stream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gönderme hatası: {ex.Message}");
        }
    }
    
    public void Close()
    {
        lock (_closeLock) // ✅ THREAD SAFETY
        {
            if (_isClosed) 
            {
                Console.WriteLine($"[Close] {AccountId} ZATEN KAPATILMIŞ - İkinci çağrı engellendi!");
                return;
            }
            _isClosed = true;
        }
        
        Console.WriteLine($"[Close] {AccountId ?? "Unknown"} kapatılıyor...");

        try
        {
            stream?.Close();
            client?.Close();
        }
        catch 
        {
            Console.WriteLine($"[Close] {AccountId} stream/client kapatma hatası");
        }

        // Oyuncu match içindeyse arena'dan çıkar
        if (PlayerData != null && PlayerData.ArenaId > 0)
        {
            Arena arena = ArenaManager.GetArena(PlayerData.ArenaId);
            arena.RemovePlayer(AccountId);
        }

        if (TeamID != 0)
        {
            LobbyManager.LeaveTeam(TeamID, AccountId);
            TeamID = 0;
        }

        if (!string.IsNullOrEmpty(AccountId))
        {
            SessionManager.RemoveSession(AccountId);
        }
        
        if (MatchMaking.waitingQueue.Contains(this))
            MatchMaking.RemoveQueue(this);

        Console.WriteLine($"[Session] {AccountId ?? "Unknown"} bağlantısı kapatıldı.");
    }
}