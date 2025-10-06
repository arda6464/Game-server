using System;

public static class ShowProfileHandler
{
    public static void Handle(Session session, byte[] data)
    {
        Console.WriteLine("show profiile handler");
        ByteBuffer byteBuffer = new ByteBuffer();
        byteBuffer.WriteBytes(data, true);
        int _ = byteBuffer.ReadInt();
        string accountId = byteBuffer.ReadString();
        byteBuffer.Dispose();
        AccountManager.AccountData account = AccountCache.Load(accountId);
        if (account == null) return;

        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.ShowProfileResponse);
        buffer.WriteString(account.AccountId);
        buffer.WriteString(account.Username);
        buffer.WriteInt(account.Namecolorid);
        buffer.WriteInt(account.Avatarid);
        byte[] veri = buffer.ToArray();
        buffer.Dispose();
        session.Send(veri);

    }
    public static void test(Session session)
    {
       ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.ShowProfileResponse);
        
        buffer.WriteString("ARDA-TEST");
        buffer.WriteInt(5);
        buffer.WriteInt(5);

        
        byte[] veri = buffer.ToArray();
        buffer.Dispose();
        session.Send(veri);
    }
}