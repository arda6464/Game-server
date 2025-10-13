using System;

public static class NotificationSender
{
    public static void Send(Session session, Notification notification)
    {
        if (session == null)
        {
            Logger.errorslog("[NotificationSender] Session null, gönderim yapılamadı.");
            return;
        }

        ByteBuffer buffer = new ByteBuffer();


        buffer.WriteInt((int)MessageType.Notification);

        buffer.WriteInt(notification.Id);
        buffer.WriteString(notification.Title);
        buffer.WriteString(notification.Message);
        buffer.WriteString(notification.ButtonText);
        buffer.WriteString(notification.Url);




        byte[] data = buffer.ToArray();
        buffer.Dispose();

        session.Send(data);

        Logger.genellog($"[NotificationSender] Bildirim gönderildi: {notification}");
    }
    public static void  İnboxSend(Session session, İnboxNotfication notification)
    {
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.Notification);

        buffer.WriteInt(notification.ID);
        buffer.WriteString(notification.Sender);
        buffer.WriteString(notification.Message);
        long unixTime = new DateTimeOffset(notification.Timespam.ToUniversalTime()).ToUnixTimeSeconds();
        buffer.WriteLong(unixTime);

     
    } 
}
