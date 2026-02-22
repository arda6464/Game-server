using System.Net;

public static class UdpHandshakeHandler
{
    public static void Handle(IPEndPoint endPoint, byte[] data)
    {
        using (ByteBuffer buffer = new ByteBuffer())
        {
            buffer.WriteBytes(data);
            short messageType = buffer.ReadShort();

            if (messageType == (short)MessageType.UdpConnect)
            {
                var packet = new UdpConnectionPacket();
                packet.Deserialize(buffer);

                // Token kontrolü
                var sessions = SessionManager.GetSessions();
                foreach (var session in sessions.Values)
                {
                    /*  if (session.ConnectionToken == packet.ConnectionToken)
                    {
                        // Eşleşme bulundu! Artık EndPoint'i paket geldikçe Server güncelleyecek.
                        session.UdpEndPoint = endPoint; 
                        Console.WriteLine($"[UDP Handshake] Başarılı: {session.Username} ({endPoint})");

                        
                        // Client'a yanıt ver (Opsiyonel ama iyi olur)
                        // session.SendUDP(new UdpConnectionResponsePacket()); // Bunu sonra ekleyebiliriz
                        return;
                    }*/
                }
                
                Console.WriteLine($"[UDP Handshake] Başarısız: Geçersiz Token ({packet.ConnectionToken}) - {endPoint}");
            }
        }
    }
}
