[PacketHandler(MessageType.ShowFriendRequest)]
public class ShowFriendRequest : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var requestPacket = data.DeserializePacket<FriendShowRequestPacket>();

        int targetId = requestPacket.TargetId;

        AccountData target = AccountCache.Load(targetId); // isteği kabul edilen kişi
        if (target == null)
        {
            Logger.errorslog($"[Friend manager] {targetId}'li hesap bulunamadı");
            return;
        }
        requestPacket.account = target;
        ByteBuffer buffer = ByteBufferPool.Get();
        requestPacket.Serialize(buffer);
        session.Send(requestPacket);
    }
}
