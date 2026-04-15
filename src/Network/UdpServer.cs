using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

public class UdpServer
{
    private UdpClient _udpClient;
    private int _port;
    private bool _isRunning;

    // Reliable UDP kaldırıldı (Artık Session içinde)
    private Thread _reliableThread;

    public UdpServer(int port)
    {
        _port = port;
        _udpClient = new UdpClient(port);
        _isRunning = true;

        _reliableThread = new Thread(ReliableLoop);
        _reliableThread.Start();
    }

    public void Start()
    {
        Console.WriteLine($"[UDP] Sunucu {_port} portunda başlatıldı.");
        _udpClient.BeginReceive(ReceiveCallback, null);
    }

    public void Stop()
    {
        _isRunning = false;
        _udpClient.Close();
        Console.WriteLine("[UDP] Sunucu durduruldu.");
    }

    private void ReceiveCallback(IAsyncResult ar)
    {

        if (!_isRunning) return;

        try
        {

            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = _udpClient.EndReceive(ar, ref clientEndPoint);

            // Hemen yeni paket dinlemeye başla
            _udpClient.BeginReceive(ReceiveCallback, null);

            // Veriyi işle
            ProcessData(clientEndPoint, data);
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                Logger.errorslog($"[UDP] Receive hatası: {ex.Message}");
                // Hata olsa bile dinlemeye devam etmeye çalış
                try { _udpClient.BeginReceive(ReceiveCallback, null); } catch { }
            }
        }
    }

    private void ProcessData(IPEndPoint clientEndPoint, byte[] data)
    {
        // 0. Ham Veri Logu
        //   Console.WriteLine($"[UDP-RAW] {data.Length} bytes received from {clientEndPoint}");

        if (data.Length < 7)
        {
            Console.WriteLine($"[UDP-ERROR] Paket çok kısa! Boyut: {data.Length} IP: {clientEndPoint}");
            return;
        }

        using (ByteBuffer buffer = new ByteBuffer())
        {
            buffer.WriteBytes(data);

            // 1. Header'ı oku
            Network.UdpPacketFlags flags = (Network.UdpPacketFlags)buffer.ReadVarInt();
            int sequenceNumber = 0;


            sequenceNumber = (int)buffer.ReadVarInt();


            int connectionToken = (int)buffer.ReadVarInt(); // VarInt'e çevrildi

            // 1. Önce IP/Port üzerinden hızlıca bulmaya çalış (O(1))
            Session? session = SessionManager.GetSessionByEndPoint(clientEndPoint);

            // 2. Eğer IP ile bulunamadıysa veya Token uyuşmuyorsa (Port/IP değişmiş olabilir) Token ile ara (O(N))
            if (session == null)
            {
                Console.WriteLine($"[UDP-DEBUG] IP üzerinden session bulunamadı, Token sorgulanıyor: {connectionToken}");
                session = SessionManager.GetSessionByConnectionToken(connectionToken);

                if (session != null)
                {

                    SessionManager.RegisterUdpSession(clientEndPoint, session);
                    Console.WriteLine($"[UDP] Session IP({clientEndPoint}) Token({connectionToken}) üzerinden YENİDEN kaydedildi: {session.Account?.Username}");
                }
                else
                {
                    Console.WriteLine($"[UDP-FAIL] Hiçbir session bulunamadı! Token: {connectionToken} IP: {clientEndPoint}");
                }
            }
            else if (session.ConnectionToken != connectionToken)
            {
                Console.WriteLine($"[UDP-WARN] IP eşleşti ama Token Hatalı! Beklenen: {session.ConnectionToken}, Gelen: {connectionToken} IP: {clientEndPoint}");
                // Token uyuşmuyorsa bu session'ı NULL yap ki alt tarafta işlem görmesin
                session = null;
            }

            if (session != null && session.ConnectionToken == connectionToken)
            {
                session.LastAlive = DateTime.Now; // ✅ UDP trafiği de artık session'ı canlı tutuyor

                // 3. Bu bir ACK paketi mi?
                if (flags.HasFlag(Network.UdpPacketFlags.Ack))
                {
                    Console.WriteLine("ack geldi");
                    session.HandleAck(sequenceNumber);
                    return;
                }

                // 4. Bu Reliable bir paket mi? Öyleyse karşıya anında ACK gönder
                if (flags.HasFlag(Network.UdpPacketFlags.Reliable))
                {
                    Console.WriteLine("reliable geldi gönderiliyor....");
                    SendAck(clientEndPoint, sequenceNumber);
                }

                // 5. Payload'u ayır
                byte[] payload = buffer.GetReadableSpan().ToArray();
                if (payload.Length == 0) return;

                MessageManager.HandleUdpMessage(session, payload, sequenceNumber);
            }
        }
    }

    // HandleAck metodu silindi (Session içinde)

    private void SendAck(IPEndPoint target, int sequenceNumber)
    {
        using (ByteBuffer buffer = new ByteBuffer())
        {
            buffer.WriteVarInt((int)Network.UdpPacketFlags.Ack);
            buffer.WriteVarInt(sequenceNumber);
            Send(target, buffer.ToArray());
        }
    }




    public void Send(IPEndPoint clientEndPoint, byte[] data)
    {
        try
        {
            // Console.WriteLine($"[UDP-SEND] {data.Length} bytes logic to {clientEndPoint}");
            _udpClient.BeginSend(data, data.Length, clientEndPoint, (ar) =>
            {
                int sent = _udpClient.EndSend(ar);
                // Console.WriteLine($"[UDP-SENT] {sent} bytes actually sent to {clientEndPoint}");
            }, null);
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[UDP] Send hatası ({clientEndPoint}): {ex.Message}");
        }
    }

    public void SendReliable(IPEndPoint clientEndPoint, byte[] fullPacketData, int seqNo, Session session)
    {
        var packet = new ReliablePacket
        {
            SequenceNumber = seqNo,
            Data = fullPacketData,
            Target = clientEndPoint,
            LastSentTime = DateTime.Now,
            RetryCount = 0
        };

        session.AddPendingPacket(seqNo, packet);
        Send(clientEndPoint, fullPacketData);
    }

    public void SendUnreliable(IPEndPoint clientEndPoint, byte[] fullPacketData)
    {
        Send(clientEndPoint, fullPacketData);
    }



    private void ReliableLoop()
    {
        while (_isRunning)
        {
            try
            {
                var now = DateTime.Now;
                var sessions = SessionManager.GetAllSessions();
                foreach (var session in sessions)
                {
                    foreach (var packet in session.GetPendingPackets())
                    {
                        if ((now - packet.LastSentTime).TotalMilliseconds > 200)
                        {
                            if (packet.RetryCount >= 5)
                            {
                                Logger.errorslog($"[UDP] Seq {packet.SequenceNumber} için 5 deneme başarısız — session kapatılıyor: {session.Account?.Username}");
                                session.HandleAck(packet.SequenceNumber); // listeden çıkar
                                // Bağlantı kopmuş say, TCP tarafını da kapat
                                Task.Run(() => session.Close());
                                continue;
                            }

                            packet.RetryCount++;
                            packet.LastSentTime = now;
                            Send(packet.Target, packet.Data);
                        }
                    }
                }
            }
            catch { }
            Thread.Sleep(50);
        }
    }
}
