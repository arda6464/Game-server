[PacketHandler(MessageType.AcceptFriendRequest)]
public class AcceptFriendRequest : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        using (var bb = ByteBufferPool.Get())
        {
            bb.WriteBytes(data, true);
            var packet = new FriendRequestAcceptPacket();
            packet.Deserialize(bb);

            if (session.Account == null)
                return;
            var target = AccountCache.Load(packet.TargetId);
            if (target == null)
            {
                Logger.errorslog($"[AcceptFriendRequest] {packet.TargetId} bulunamadı");
                return;
            }

            FriendsManager.AcceptRequest(session.Account, target, session);
        }
    }
}
