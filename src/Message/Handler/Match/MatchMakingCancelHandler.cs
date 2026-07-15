using System;

[PacketHandler(MessageType.MatchMakingCancelRequest)]
public class MatchMakingCancelHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        MatchMaking.RemoveQueue(session);
    }
}
