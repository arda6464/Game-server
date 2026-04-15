using System;


public static class Loginfailed
{
    public static void Send(Session session, string erormessage, int erorid)
    {
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteVarInt((int)MessageType.LoginFailed);
        buffer.WriteVarInt(erorid);
        buffer.WriteString(erormessage);
        byte[] veri = buffer.ToArray();
        buffer.Dispose();
        session.Send(veri);
        session.Close();
        Console.WriteLine("send loginfailed sebep: " + erormessage);
    }
}
