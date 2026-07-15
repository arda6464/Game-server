[PacketHandler(MessageType.BestFriendChanged)]
public class BestFriendHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        using (var bb = ByteBufferPool.Get())
        {
            bb.WriteBytes(data);
            bb.ReadVarInt();
            int targetId = bb.ReadVarInt();

            if (session.Account == null) return;
            FriendsManager.SetBestFriend(session.Account, targetId);
        }
    }
}
