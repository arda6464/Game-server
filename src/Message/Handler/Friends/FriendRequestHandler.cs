[PacketHandler(MessageType.SendFriendRequest)]
public class FriendRequestHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        using (var bb = ByteBufferPool.Get())
        {
            bb.WriteBytes(data, true);
            var packet = new SendFriendRequestPacket();
            packet.Deserialize(bb);

            if (session.Account == null)
                return;
            var target = AccountCache.Load(packet.TargetId);
            if (target == null)
            {
                Logger.errorslog($"[SendFriendRequest] {packet.TargetId} bulunamadı");
                return;
            }

            FriendsManager.SendRequest(session.Account, target);
        }
    }
}
