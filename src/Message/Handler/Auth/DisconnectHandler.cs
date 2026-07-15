using System;

[PacketHandler(MessageType.Disconnect)]
public class DisconnectHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        session.Close();
    }
}
