using Logic;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;


public class Session
{
    public PlayerState State { get; private set; } = PlayerState.None;
    private TcpClient client;
    private NetworkStream stream;
    public Player? PlayerData { get; set; }
    public AccountManager.AccountData? Account { get; set; }
    public string AccountId { get; set; }
    public DateTime LastPingSent { get; set; }
    public DateTime LastAlive { get; set; }
    public int LastPing { get; set; }
    public string DeviceID { get; set; }
    public string IP { get; set; }
    public int TeamID = 0;
    public string FBNToken = null;
    public int BattleId = 0;
    
    // Reliable UDP
    private ushort _udpSequenceCounter = 0;
    private ConcurrentDictionary<ushort, ReliablePacket> _pendingPackets = new();

    public ushort GetNextSequence() => _udpSequenceCounter++;
    public void HandleAck(ushort seq) => _pendingPackets.TryRemove(seq, out _);
    public void AddPendingPacket(ushort seq, ReliablePacket packet) => _pendingPackets[seq] = packet;
    public IEnumerable<ReliablePacket> GetPendingPackets() => _pendingPackets.Values;

    private bool _isClosed = false;
    private object _closeLock = new object();

    public void ChangeState(PlayerState newState)
    {
        if (State == newState) return;

        PlayerState oldState = State;
        State = newState;

        Console.WriteLine($"[Session] {AccountId ?? "Bilinmeyen"} durum değiştirdi: {oldState} -> {newState}");

        switch (newState)
        {
            case PlayerState.Lobby:
                Logic.LobbyLogic.HomeVisited(this);
                break;
            case PlayerState.Battle:
               
                break;
        }
    }

   

   

    public IPEndPoint? UdpEndPoint { get; set; }
    public int ConnectionToken { get; set; }

    public Session(TcpClient c)
    {
        client = c;
        client.NoDelay = true; // ✅ Nagle algoritmasını kapatarak ping sıçramalarını engelliyoruz
        this.stream = client.GetStream();
        // Rastgele bir token oluştur (UDP Handshake için) - HashCode kullanarak int elde ediyoruz
        ConnectionToken = Guid.NewGuid().GetHashCode();
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

                try
                {
                    LastAlive = DateTime.Now; // ✅ Herhangi bir paket geldiğinde 'hayatta' olduğunu işaretle
                    MessageManager.HandleMessage(this, buffer);
                }
                catch (Exception ex)
                {
                    Logger.errorslog($"[Session] Message handler hatası ({AccountId}): {ex.Message}\n{ex.StackTrace}");
                    // Hata loglanır ama sunucu çökmez
                }
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
    
    public void Send(IPacket packet)
    {
        using (ByteBuffer buffer = new ByteBuffer())
        {
            packet.Serialize(buffer);
            Send(buffer.ToArray());
        }
    }

    public void Send(byte[] buffer)
    {
        if (!client.Connected)
        {
            throw new InvalidOperationException("Bağlantı kapalı");
        }

        // Trafiği kaydet
        TrafficMonitor.RecordOutgoing(buffer);

        try
        {
            stream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gönderme hatası: {ex.Message}");
        }
    }
    
    public void SendUnreliableUDP(byte[] buffer)
    {
        if (UdpEndPoint == null) return;
        GameServer.UdpServer?.SendUnreliable(UdpEndPoint, buffer);
    }

    public void SendReliableUDP(byte[] buffer, ushort seqNo)
    {
        if (UdpEndPoint == null) return;
        GameServer.UdpServer?.SendReliable(UdpEndPoint, buffer, seqNo, this);
    }




    public void Close()
    {
        lock (_closeLock) // ✅ THREAD SAFETY
        {
            if (_isClosed) 
            {
              //  Console.WriteLine($"[Close] {AccountId} ZATEN KAPATILMIŞ - İkinci çağrı engellendi!");
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
         PlayerSetPresence.Handle(this, PlayerSetPresence.PresenceState.Offline);
        // Oyuncu maç içindeyse savaştan çıkar
        if (PlayerData != null && PlayerData.BattleId > 0)
        {
            Battle battle = ArenaManager.GetBattle(PlayerData.BattleId);
            battle?.RemovePlayer(AccountId);
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

        // Bellek güvenliği için referansları koparalım
        this.Account = null;
        this.PlayerData = null;
    }
}