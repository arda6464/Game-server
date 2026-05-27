[PacketHandler(MessageType.ShowFriendRequest)]
public static class ShowFriendRequest
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer byteBuffer = ByteBufferPool.Get();
        byteBuffer.WriteBytes(message, true);

        var requestPacket = new FriendShowRequestPacket();
        requestPacket.Deserialize(byteBuffer);

        int targetId = requestPacket.TargetId;
        byteBuffer.Dispose();




        AccountManager.AccountData target = AccountCache.Load(targetId); // isteği kabul edilen kişi
        if (target == null)
        {
            Logger.errorslog($"[Friend manager] {targetId}'li hesap bulunamadı");
            return;
        }
        requestPacket.account  = target;
       ByteBuffer buffer =  ByteBufferPool.Get();
        requestPacket.Serialize(buffer);
        buffer.Dispose();
        session.Send(requestPacket);
        

    }
}
