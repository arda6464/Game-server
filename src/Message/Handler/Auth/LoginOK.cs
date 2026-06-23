public static class LoginOK
{
    public static void Handle(Session session, string newtoken,int newid)
    {
        var account = session.Account;
        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            buffer.WriteVarInt((int)MessageType.LoginOKResponse);
            buffer.WriteVarString(newtoken);
            buffer.WriteVarInt(newid);
            session.Send(buffer.GetBufferSegment());
        }

        // napcaz ki yaaa 
    }
}
