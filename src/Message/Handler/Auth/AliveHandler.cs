using System;

[PacketHandler(MessageType.Alive)]
public static class AliveHandler
{
    public static void Handle(Session session)
    {
        session.LastAlive = DateTime.Now;
    }
}
