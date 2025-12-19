public static class PingHandler
{
    public static void Handle(Session session,byte[] data)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(data, true);
        int _ = read.ReadInt();

        double clientsenttime = read.ReadDouble();
        int lastping = read.ReadInt();
        read.Dispose();


      
        session.LastPing = lastping;
            
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

        string message = $"Test Server\n Online oyuncu: {SessionManager.Count()}\n   {session.LastPing} Ms";
       // Console.WriteLine(message);
        ByteBuffer buffer = new ByteBuffer();
        
        buffer.WriteInt((int)MessageType.Pong);
        buffer.WriteString(message);
        buffer.WriteDouble(clientsenttime);
        byte[] seks = buffer.ToArray();
        buffer.Dispose();
        session.Send(seks);

       
    }
}