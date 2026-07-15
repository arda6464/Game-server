using System;


public static class LoginFailedHandler
{
    public static void Send(Session session, string erormessage, int erorid)
    {
        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            buffer.WriteVarInt((int)MessageType.Loginfailed);
            buffer.WriteVarInt(erorid);
            buffer.WriteVarString(erormessage);
            session.Send(buffer.GetBufferSegment());
        }
        session.Close();
        Console.WriteLine("send LoginFailedHandler sebep: " + erormessage);
    }
}
