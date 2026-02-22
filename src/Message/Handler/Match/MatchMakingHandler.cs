using System;

[PacketHandler(MessageType.MatchMakingRequest)]
public static class MatchMakingHandler
{
    public static void Handle(Session session)
    {
        MatchMaking.JoinQueue(session);
    }
}
