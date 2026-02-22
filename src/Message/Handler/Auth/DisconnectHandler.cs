using System;

[PacketHandler(MessageType.Disconnect)]
public static class DisconnectHandler
{
    public static void Handle(Session session)
    {
        session.Close();
    }
}
