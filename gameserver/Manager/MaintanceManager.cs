public static class Maintance
{
    public static bool MaintanceMode = false;
    public static DateTime FinishTime;
    public static int PanicMS = 5000;
    public static int NormalMS = 60000;

    public static void StartMaintance(TimeSpan finish,bool PanicMode = false)
    {
        MaintanceMode = true;
        FinishTime = DateTime.Now.Add(finish);
        if (PanicMode)
        {
            Logger.genellog($"[Maintance] bakım molası aktif panicmode: {(PanicMode ? "Acil" : "Normal")}");
            Notification notification = new Notification
            {
                Id = 11,
                Title = "Bakım molası",
                Message = "Sunucumuz bakım molasına giriyor",
                iconid = 4
            };
            var sessions = SessionManager.GetSessions().Values;
            foreach (var session in sessions)
            {
                NotificationSender.Send(session, notification);
            }
            Thread.Sleep(PanicMS);
            foreach (var session in sessions)
            {
                Loginfailed.Send(session, "Bakım molası!", 1);
            }
            Logger.genellog("[Maintance] bakım modune girildi aktif oturum sayısı: " + SessionManager.GetSessions().Count);

        }
        else
        {
            Logger.genellog($"[Maintance] bakım molası aktif Mode: {(PanicMode ? "Acil" : "Normal")}");
            Notification notification = new Notification
            {
                Id = 11,
                Title = "Bakım molası",
                Message = "Sunucumuz bakım molasına giriyor",
                iconid = 4
            };
            var sessions = SessionManager.GetSessions().Values;
            foreach (var session in sessions)
            {
                NotificationSender.Send(session, notification);
            }
            Thread.Sleep(NormalMS);
            foreach (var session in sessions)
            {
                Loginfailed.Send(session, "Bakım molası!", 1);
            }
            Logger.genellog("[Maintance] bakım modune girildi aktif oturum sayısı: " + SessionManager.GetSessions().Count);

        }
    }
    public static void SendMaintancePacket(Session session)
    {
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.Maintance);
        long unixTime = new DateTimeOffset(FinishTime.ToUniversalTime()).ToUnixTimeSeconds();
        buffer.WriteLong(unixTime);
        byte[] bytes = buffer.ToArray();
        buffer.Dispose();
        session.Send(bytes);
    }
    public static void finishMaintence()
    {
        MaintanceMode = false;
        Logger.genellog("[Maintance] bakım başarıyla sona erdi");
    }

}