using System;

[PacketHandler(MessageType.ShowProfileRequest)]
public static class ShowProfileHandler
{
    public static void Handle(Session session, byte[] data)
    {
        Console.WriteLine("show profiile handler");
        ByteBuffer byteBuffer = new ByteBuffer();
        byteBuffer.WriteBytes(data, true);

        var request = new ShowProfileRequestPacket();
        request.Deserialize(byteBuffer);
        
        AccountManager.AccountData? account = null;
       
            account = AccountCache.Load(request.ID);
        
        
        if (account == null) return;

        var response = new ShowProfileResponsePacket
        {
            account = account
        };
        session.Send(response);
        Console.WriteLine("profile response gönderildi: " + response);

    }
    public static void test(Session session)
    {
       ByteBuffer buffer = new ByteBuffer();
        buffer.WriteVarInt((int)MessageType.ShowProfileResponse);
        
        buffer.WriteString("ARDA-TEST");
        buffer.WriteVarInt(5);
        buffer.WriteVarInt(5);

        
        byte[] veri = buffer.ToArray();
        buffer.Dispose();
        session.Send(veri);
        
    } 
}
