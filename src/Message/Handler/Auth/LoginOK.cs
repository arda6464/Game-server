public static class LoginOK
{
    public static void Handle(Session session, string newtoken,string newid)
    {
        var account = session.Account;
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteShort((short)MessageType.LoginOKResponse);
        buffer.WriteString(newtoken);
        buffer.WriteString(newid);
        byte[] response = buffer.ToArray();
        buffer.Dispose();
        session.Send(response);

        // napcaz ki yaaa 
    }
}
