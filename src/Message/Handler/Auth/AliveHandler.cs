using System;

[PacketHandler(MessageType.Alive)]
public class AliveHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        session.LastAlive = DateTime.Now;
    }
}
