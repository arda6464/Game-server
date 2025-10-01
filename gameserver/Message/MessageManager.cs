using System;

public static class MessageManager
{
    public static void HandleMessage(Session session, byte[] data)
    {
        int value = BitConverter.ToInt32(data, 0);
        switch ((MessageType)value)
        {
            case MessageType.FirstConnectionRequest:
                FirstConnectionHandler.Handle(session, data);
                break;
            case MessageType.AuthLoginRequest:
                AuthLoginHandler.Handle(session, data);
                break;
            case MessageType.Disconnect:
                session.Close();
                break;
        }
    }


    
}