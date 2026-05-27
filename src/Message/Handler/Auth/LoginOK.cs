public static class LoginOK
{
    public static void Handle(Session session, string newtoken,int newid)
    {
        var account = session.Account;
        ByteBuffer buffer = ByteBufferPool.Get();
        buffer.WriteVarInt((int)MessageType.LoginOKResponse);
        buffer.WriteVarString(newtoken);
        buffer.WriteVarInt(newid);
        byte[] response = buffer.ToArray();
        buffer.Dispose();
        session.Send(response);

        // napcaz ki yaaa 
    }
}
