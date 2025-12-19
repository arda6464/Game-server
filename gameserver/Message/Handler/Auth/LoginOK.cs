public static class LoginOK
{
    public static void Handle(Session session, string newtoken,string newid)
    {
        var account = AccountCache.Load(session.AccountId);
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.LoginOKResponse);
        buffer.WriteString(newtoken);
        buffer.WriteString(newid);
        byte[] response = buffer.ToArray();
        buffer.Dispose();
        session.Send(response);

        // napcaz ki yaaa 
    }
}
