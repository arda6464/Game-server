public static class Maintenance
{
    public static bool MaintenanceMode = false;
    public static DateTime FinishTime;
    public static int PanicMS = 5000;
    public static int NormalMS = 60000;

    public static void StartMaintenance(TimeSpan finish,bool PanicMode = false)
    {
        MaintenanceMode = true;
        FinishTime = DateTime.Now.Add(finish);
        if (PanicMode)
        {
            Logger.genellog($"[Maintenance] bakım molası aktif panicmode: {(PanicMode ? "Acil" : "Normal")}");
            Notification notification = new Notification
            {
                 type =  NotificationTypes.NotificationType.toast,
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
            Logger.genellog("[Maintenance] bakım modune girildi aktif oturum sayısı: " + SessionManager.GetSessions().Count);

        }
        else
        {
            Logger.genellog($"[Maintenance] bakım molası aktif Mode: {(PanicMode ? "Acil" : "Normal")}");
            Notification notification = new Notification
            {
                 type  =  NotificationTypes.NotificationType.toast,
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
            Logger.genellog("[Maintenance] bakım modune girildi aktif oturum sayısı: " + SessionManager.GetSessions().Count);

        }
    }
    public static void SendMaintenancePacket(Session session)
    {
        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            buffer.WriteVarInt((int)MessageType.Maintenance);
            long unixTime = new DateTimeOffset(FinishTime.ToUniversalTime()).ToUnixTimeSeconds();
            buffer.WriteVarLong(unixTime);
            session.Send(buffer.GetBufferSegment());
        }
    }
    public static void finishMaintenance()
    {
        MaintenanceMode = false;
        Logger.genellog("[Maintenance] bakım başarıyla sona erdi");
    }

}