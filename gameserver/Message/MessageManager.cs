using System;

public static class MessageManager
{
    public static void HandleMessage(Session session, byte[] data)
    {
        int value = BitConverter.ToInt32(data, 0);
        switch ((MessageType)value)
        {
            case MessageType.FirstConnection:
                FirstConnectionHandler.Handle(session, data);
                break;
        }
    }


    
}