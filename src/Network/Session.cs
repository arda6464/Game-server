using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Logic;

public class Session : IDisposable
{
    public PlayerState State { get; private set; } = PlayerState.None;
    private TcpClient client;
    private NetworkStream stream;
    private byte[]? _initialData;
    public Player? PlayerData { get; set; }
    public bool IsConnected => client != null && client.Connected;
    private AccountData? _account;
    public AccountData? Account
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
    private object _closeLock = new object();

    public void ChangeState(PlayerState newState)
    {
        if (State == newState)
            return;

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

    public async Task StartAsync()
    {
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
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
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

            // Sıfır kopyalama ve sıfır array allocation!
            MessageManager.HandleMessage(this, buffer, length);
        }
        catch (Exception ex)
        {
            Logger.errorslog(
                $"[Session] Message handler hatası ({ID}): {ex.Message}\n{ex.StackTrace}"
            );
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

    public void Send<T>(T packet)
        where T : IPacket
    {
        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            packet.Serialize(buffer);
            Send(buffer.GetBufferSegment());
        }
    }

    public void Send(byte[] buffer)
    {
        if (!client.Connected)
        {
            throw new InvalidOperationException("Bağlantı kapalı");
        }

        // Trafiği kaydet
        TrafficMonitor.RecordOutgoingRaw(buffer.Length);

        try
        {
            stream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gönderme hatası: {ex.Message}");
        }
    }

    public void Send(ArraySegment<byte> segment)
    {
        if (!client.Connected)
        {
            throw new InvalidOperationException("Bağlantı kapalı");
        }

        // Trafiği kaydet
        TrafficMonitor.RecordOutgoingRaw(segment.Count);

        try
        {
            stream.Write(segment);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gönderme hatası: {ex.Message}");
        }
    }

    private bool _isClosed = false;

    public void Close()
    {
        lock (_closeLock)
        {
            if (_isClosed)
                return;
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
        MatchMaking.RemoveQueue(this);
        PlayerSetPresence.Handle(this, PlayerSetPresence.PresenceState.Offline);

        if (BattleId > 0)
        {
            GameServer.BattleServer?.RemovePeer(BattleId);
            BattleId = 0;
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

        Console.WriteLine($"[Session] {ID} bağlantısı kapatıldı.");

        this.Account = null;
        this.PlayerData = null;
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}
