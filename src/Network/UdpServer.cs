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
        if (data.Length < 7) return; // En az 7 byte Header olmalı (Flags(1) + Seq(2) + Token(4))

        using (ByteBuffer buffer = new ByteBuffer())
        {
            buffer.WriteBytes(data);
            
            // 1. Header'ı oku
            Network.UdpPacketFlags flags = (Network.UdpPacketFlags)buffer.ReadByte();
            ushort sequenceNumber = (ushort)buffer.ReadShort();
            int connectionToken = buffer.ReadInt(); // 4 Byte'lık Token

            // Oturumu Token bazlı bul
            Session session = SessionManager.GetSessionByConnectionToken(connectionToken);
            
            if (session != null)
            {
                // Port değişmişse EndPoint'i güncelle (NAT/Port değişimi desteği)
                if (session.UdpEndPoint == null || !session.UdpEndPoint.Equals(clientEndPoint))
                {
                    session.UdpEndPoint = clientEndPoint;
                }

                // 2. Bu bir ACK paketi mi?
                if (flags.HasFlag(Network.UdpPacketFlags.Ack))
                {
                    // Session bazlı ACK işle
                    session.HandleAck(sequenceNumber);
                    return; 
                }

                // 3. Bu Reliable bir paket mi? Öyleyse karşıya anında ACK gönder
                if (flags.HasFlag(Network.UdpPacketFlags.Reliable))
                {
                    SendAck(clientEndPoint, sequenceNumber);
                }

                // 4. Payload'u ayır (Geri kalan 7. byte'tan sonrası asıl verimizdir)
                byte[] payload = buffer.ToArray().Skip(7).ToArray();

                if (payload.Length == 0) return;

                MessageManager.HandleUdpMessage(session, payload, sequenceNumber);
            }
            else
            {
                // Eğer oturum yoksa, bu bir Handshake paketi olabilir
                // Handshake paketi de standart UDP header'ına (7 byte) sahip olmalı.
                byte[] payload = buffer.ToArray().Skip(7).ToArray();
                if (payload.Length > 0)
                {
                     UdpHandshakeHandler.Handle(clientEndPoint, payload);
                }
            }
        }
    }

    // HandleAck metodu silindi (Session içinde)

    private void SendAck(IPEndPoint target, ushort sequenceNumber)
    {
        using (ByteBuffer buffer = new ByteBuffer())
        {
            buffer.WriteByte((byte)Network.UdpPacketFlags.Ack);
            buffer.WriteShort((short)sequenceNumber);
            Send(target, buffer.ToArray());
        }
    }




    public void Send(IPEndPoint clientEndPoint, byte[] data)
    {
        try
        {
            _udpClient.BeginSend(data, data.Length, clientEndPoint, (ar) => 
            {
                _udpClient.EndSend(ar);
            }, null);
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[UDP] Send hatası ({clientEndPoint}): {ex.Message}");
        }
    }

    public void SendReliable(IPEndPoint clientEndPoint, byte[] fullPacketData, ushort seqNo, Session session)
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
                                session.HandleAck(packet.SequenceNumber);
                                continue;
                            }
                            
                            packet.RetryCount++;
                            packet.LastSentTime = now;
                            Send(packet.Target, packet.Data);
                        }
                    }
                }
            }
            catch {}
            Thread.Sleep(50);
        }
    }
}
