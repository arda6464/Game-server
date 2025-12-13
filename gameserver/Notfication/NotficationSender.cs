using System;

public static class NotficationSender
{
    public static void Send(Session session, Notfication notification)
    {
        if (session == null)
        {
            Logger.errorslog("[NotificationSender] Session null, gönderim yapılamadı.");
            return;
        }

        ByteBuffer buffer = new ByteBuffer();


        buffer.WriteInt((int)MessageType.Notification);
        buffer.WriteInt(notification.Id);
        switch(notification.Id)
        {
            case 11:
                {
                    
                    buffer.WriteString(notification.Title);
                    buffer.WriteString(notification.Message);
                    buffer.WriteInt(notification.iconid);
                }
                break;
            case 10:
                {
                    
                    buffer.WriteString(notification.Title?? "");
                    buffer.WriteString(notification.Message ?? "");
                    buffer.WriteString(notification.ButtonText ?? " ");
                    buffer.WriteString(notification.Url ?? " ");
                }
                break;
            case 12:
                {
                      buffer.WriteString(notification.Sender);
                    buffer.WriteString(notification.Message);
                    buffer.WriteBool(notification.IsViewed);
                    long unixTime = new DateTimeOffset(notification.Timespam.ToUniversalTime()).ToUnixTimeSeconds();
                    buffer.WriteLong(unixTime);
                }
                break;
                
        }
       
    
       




        byte[] data = buffer.ToArray();
        buffer.Dispose();

        session.Send(data);

        Logger.genellog($"[NotificationSender] Bildirim gönderildi: {notification.Message}");
    }
    
   
}
