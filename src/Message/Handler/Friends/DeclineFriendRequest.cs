[PacketHandler(MessageType.DeclineFriendRequest)]
public class DeclineFriendRequest : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        using (var bb = ByteBufferPool.Get())
        {
            bb.WriteBytes(data, true);
            var packet = new FriendRequestDeclinePacket();
            packet.Deserialize(bb);

            if (session.Account == null)
                return;
            var target = AccountCache.Load(packet.TargetId);
            if (target == null)
            {
                Logger.errorslog("[DeclineFriendRequest] hedef hesap bulunamadı");
                return;
            }

            FriendsManager.DeclineRequest(session.Account, target);
        }
    }
}
