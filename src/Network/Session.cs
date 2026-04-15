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
    private byte[]? _initialData;
    public Player? PlayerData { get; set; }
    public bool IsConnected => client != null && client.Connected;
    private AccountManager.AccountData? _account;
    public AccountManager.AccountData? Account 
    { 
        get => _account; 
        set 
        {
            _account = value;
            if (_account != null)
            {
                Logic = new AccountLogic(_account, this);
            }
        }
    }
    public AccountLogic? Logic { get; private set; }
    public int ID { get; set; }
    public string Token { get; set; }
    public DateTime LastPingSent { get; set; }
    public DateTime LastAlive { get; set; }
    public int LastPing { get; set; }
    public string DeviceID { get; set; }
    public string IP { get; set; }
    public int TeamID = 0;
    public string FBNToken = null;
    public int BattleId = 0;

    // Reliable UDP
    private int  _reliableSeqCounter = 0;
    private int  _unreliableSeqCounter = 0;
    private ConcurrentDictionary<int, ReliablePacket> _pendingPackets = new();

    // Sadece unreliable (move/input/snapshot) paketlerin drop kontrolü için
    public int LastIncomingUnreliableSeq { get; private set; } = 0;

    public bool IsNewUnreliableSequence(int seqNo)
    {
        // ushort wrap-around (65535 -> 0) durumunu kontrol et
        if (seqNo > LastIncomingUnreliableSeq)
        {
            LastIncomingUnreliableSeq = seqNo;
            return true;
        }
        return false;
    }

    /// <summary>Reliable paketler için seqNo (Connect, Shoot vb.)</summary>
    public int GetNextReliableSequence() => _reliableSeqCounter++;

    /// <summary>Unreliable paketler için seqNo (Move, Input, Snapshot vb.)</summary>
    public int GetNextUnreliableSequence() => _unreliableSeqCounter++;
    public void HandleAck(int seq)
    {
        bool wasPending = _pendingPackets.TryRemove(seq, out _);
        if (wasPending)
        {
            // İsteğe bağlı: Başarıyla ACK alındığını loglayabilirsin
            Console.WriteLine($"[UDP] Seq {seq} için ACK alındı ve listeden çıkarıldı.");
        }
        else
        {
            // İsteğe bağlı: Beklenmeyen veya zaten işlenmiş bir ACK gelirse loglayabilirsin
            Console.WriteLine($"[UDP-WARN] Seq {seq} için ACK geldi ama bekleyen listesinde YOKTU. (Zaten işlenmiş veya geçersiz olabilir)");
        }

    }
    public void AddPendingPacket(int seq, ReliablePacket packet) => _pendingPackets[seq] = packet;
    public IEnumerable<ReliablePacket> GetPendingPackets() => _pendingPackets.Values;

    private bool _isClosed = false;
    private object _closeLock = new object();

    public void ChangeState(PlayerState newState)
    {
        if (State == newState) return;

        PlayerState oldState = State;
        State = newState;

        Console.WriteLine($"[Session] {ID} durum değiştirdi: {oldState} -> {newState}");

        switch (newState)
        {
            case PlayerState.Lobby:
                Logic?.HomeVisited();
                break;
            case PlayerState.Battle:

                break;
        }
    }

    public IPEndPoint? UdpEndPoint { get; set; }
    public int ConnectionToken { get; set; }

    public Session(TcpClient c, byte[]? initialData = null)
    {
        client = c;
        client.NoDelay = true; // ✅ Nagle algoritmasını kapatarak ping sıçramalarını engelliyoruz
        this.stream = client.GetStream();
        this._initialData = initialData;
        // Rastgele bir pozitif token oluştur (UDP Handshake için)
        ConnectionToken = Guid.NewGuid().GetHashCode() & int.MaxValue;
    }

    public void Start()
    {
        // Eğer multiplexer'dan gelen başlangıç verisi varsa önce onu işle
        if (_initialData != null && _initialData.Length > 0)
        {
            HandleRawData(_initialData, _initialData.Length);
            _initialData = null;
        }

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

                HandleRawData(buffer, bytesRead);
            }
        }
        finally
        {
            Close();
        }
    }

    private void HandleRawData(byte[] buffer, int length)
    {
        try
        {
            LastAlive = DateTime.Now; // ✅ Herhangi bir paket geldiğinde 'hayatta' olduğunu işaretle
            
            // Eğer buffer'ın tamamı dolu değilse (length < buffer.Length), 
            // HandleMessage'in doğru boyutta veri aldığından emin olmalıyız.
            if (length < buffer.Length)
            {
                byte[] data = new byte[length];
                Array.Copy(buffer, 0, data, 0, length);
                MessageManager.HandleMessage(this, data);
            }
            else
            {
                MessageManager.HandleMessage(this, buffer);
            }
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Session] Message handler hatası ({ID}): {ex.Message}\n{ex.StackTrace}");
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

    public void SendUnreliableUDP(IPacket packet) // Yeni eklenen metod - Otomatik Header
    {
        if (UdpEndPoint == null) return;
        int seqNo = GetNextUnreliableSequence();

        using (ByteBuffer finalBuffer = new ByteBuffer())
        {
            finalBuffer.WriteVarInt((int)Network.UdpPacketFlags.None);
            finalBuffer.WriteVarInt(seqNo);
            packet.Serialize(finalBuffer);
            GameServer.UdpServer?.SendUnreliable(UdpEndPoint, finalBuffer.ToArray());
        }
    }

    // Toplu yayın (Broadcast) optimizasyonu: Payload 1 kez oluşturulup buraya verilir.
    public void SendUnreliableUDP_Payload(byte[] payloadData)
    {
        if (UdpEndPoint == null) return;
        int seqNo = GetNextUnreliableSequence();

        using (ByteBuffer finalBuffer = new ByteBuffer())
        {
            finalBuffer.WriteVarInt((int)Network.UdpPacketFlags.None);
            finalBuffer.WriteVarInt(seqNo);
            finalBuffer.WriteBytes(payloadData, false); // Sadece sonuna ekle
            GameServer.UdpServer?.SendUnreliable(UdpEndPoint, finalBuffer.ToArray());
        }
    }

    public void SendReliableUDP(byte[] buffer, int seqNo)
    {
        if (UdpEndPoint == null) return;
        GameServer.UdpServer?.SendReliable(UdpEndPoint, buffer, seqNo, this);
    }

    public void SendReliableUDP(IPacket packet) // Yeni eklenen metod - Otomatik Header
    {
        if (UdpEndPoint == null) return;
        int seqNo = GetNextReliableSequence();

        using (ByteBuffer finalBuffer = new ByteBuffer())
        {
            finalBuffer.WriteVarInt((int)Network.UdpPacketFlags.Reliable);
            finalBuffer.WriteVarInt(seqNo);
            packet.Serialize(finalBuffer);
            GameServer.UdpServer?.SendReliable(UdpEndPoint, finalBuffer.ToArray(), seqNo, this);
        }
    }

    public int GetConnenctToken()
    {
        if (ConnectionToken != 0)
            return ConnectionToken;

        Console.WriteLine("hata: connection token oluşturulmadığı için sıfırdan oluşturuldu");
        ConnectionToken = Guid.NewGuid().GetHashCode() & int.MaxValue;
        return ConnectionToken;
    }

    public void Close()
    {
        lock (_closeLock) // ✅ THREAD SAFETY
        {
            if (_isClosed)
            {
                return;
            }
            _isClosed = true;
        }

        Console.WriteLine($"[Close] {ID} kapatılıyor...");

        try
        {
            stream?.Close();
            client?.Close();
        }
        catch
        {
            Console.WriteLine($"[Close] {ID} stream/client kapatma hatası");
        }
        PlayerSetPresence.Handle(this, PlayerSetPresence.PresenceState.Offline);
        // Oyuncu maç içindeyse savaştan çıkar
        if (PlayerData != null && PlayerData.BattleId > 0)
        {
            Battle battle = ArenaManager.GetBattle(PlayerData.BattleId);
            battle?.RemovePlayer(ID);
        }

        if (TeamID != 0)
        {
            LobbyManager.LeaveTeam(TeamID, ID);
            TeamID = 0;
        }

        if (ID != 0)
        {
            SessionManager.RemoveSession(ID);
        }

        SessionManager.UnRegisterUdpSession(UdpEndPoint);

        Console.WriteLine($"[Session] {ID} bağlantısı kapatıldı.");

        // Bellek güvenliği için referansları koparalım
        this.Account = null;
        this.PlayerData = null;
    }
}