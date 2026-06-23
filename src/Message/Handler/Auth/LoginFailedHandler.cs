using System;


public static class Loginfailed
{
    public static void Send(Session session, string erormessage, int erorid)
    {
        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            buffer.WriteVarInt((int)MessageType.LoginFailed);
            buffer.WriteVarInt(erorid);
            buffer.WriteString(erormessage);
            session.Send(buffer.GetBufferSegment());
        }
        session.Close();
        Console.WriteLine("send loginfailed sebep: " + erormessage);
    }
}
