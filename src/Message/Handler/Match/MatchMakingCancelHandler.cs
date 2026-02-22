using System;

[PacketHandler(MessageType.MatchMakingCancelRequest)]
public static class MatchMakingCancelHandler
{
    public static void Handle(Session session)
    {
        MatchMaking.RemoveQueue(session);
    }
}
