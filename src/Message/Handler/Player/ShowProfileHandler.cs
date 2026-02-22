using System;

[PacketHandler(MessageType.ShowProfileRequest)]
public static class ShowProfileHandler
{
    public static void Handle(Session session, byte[] data)
    {
        Console.WriteLine("show profiile handler");
        ByteBuffer byteBuffer = new ByteBuffer();
        byteBuffer.WriteBytes(data, true);
        int _ = byteBuffer.ReadShort();
        
        var request = new ShowProfileRequestPacket();
        request.Deserialize(byteBuffer);
        
        string accountId = request.AccountId;
        byteBuffer.Dispose();
        
        AccountManager.AccountData account = AccountCache.Load(accountId);
        if (account == null) return;

        var response = new ShowProfileResponsePacket
        {
            AccountId = account.AccountId,
            Username = account.Username,
            NameColorId = account.Namecolorid,
            AvatarId = account.Avatarid
        };
        session.Send(response);

    }
    public static void test(Session session)
    {
       ByteBuffer buffer = new ByteBuffer();
        buffer.WriteShort((short)MessageType.ShowProfileResponse);
        
        buffer.WriteString("ARDA-TEST");
        buffer.WriteInt(5);
        buffer.WriteInt(5);

        
        byte[] veri = buffer.ToArray();
        buffer.Dispose();
        session.Send(veri);
    }
}