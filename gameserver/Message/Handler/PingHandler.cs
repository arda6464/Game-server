public static class PingHandler
{
    public static void Handle(Session session,byte[] data)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(data, true);
        int _ = read.ReadInt();
        
        long clientsenttime = read.ReadLong();
        read.Dispose();


        long servertime = DateTime.UtcNow.AddHours(3).Ticks;
        long roundTripTime = (servertime - clientsenttime) / TimeSpan.TicksPerMillisecond;
        session.LastPing = (int)roundTripTime;
            
           string str = "▂   ";
            if (session.LastPing <= 75)
            {
                str = "▂▄▆█";
            }
            else if (session.LastPing <= 125)
            {
                str = "▂▄▆ ";
            }
            else if (session.LastPing <= 300)
            {
                str = "▂▄  ";
            }
        
        ByteBuffer buffer = new ByteBuffer();
           
        buffer.WriteInt((int)MessageType.Pong);
        buffer.WriteString($"Test Server\n Online oyuncu: {SessionManager.Count()}\n  {str} {session.LastPing} Ms");
        byte[] seks = buffer.ToArray();
        buffer.Dispose();
        session.Send(seks);
        Console.WriteLine($"Test Server\n Online oyuncu: {SessionManager.Count()}\n Ping: {session.LastPing}");
    }
}