[PacketHandler(MessageType.Ping)]
public static class PingHandler
{
    public static void Handle(Session session, byte[] data)
    {
        using (ByteBuffer read = new ByteBuffer())
        {
            read.WriteBytes(data, true);
            
            // 1. Gelen veriyi oku (Deserialize)
            PingPacket request = new PingPacket();
            request.Deserialize(read);

            // 2. İşlemleri yap
            session.LastPing = request.LastPing;
            session.LastPingSent = DateTime.Now;

           

            // 3. Cevabı hazırla ve gönder (Serialize)
            PongPacket response = new PongPacket
            {
                ClientSentTime = request.ClientSentTime
            };
            
            session.Send(response);
        }
    }
}
