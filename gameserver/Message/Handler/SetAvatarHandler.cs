using System;

public static class SetAvatar
{
    public static void Handle(Session session, byte[] data)
    {

        Console.WriteLine("Set Avatar");
        ByteBuffer BUFFER = new ByteBuffer();
        BUFFER.WriteBytes(data, true);
        int _ = BUFFER.ReadInt();

        int Id = BUFFER.ReadInt();
        BUFFER.Dispose();
        
        // Avatar ID validasyonu (1-10 arası)
        if (Id < 1 || Id > 10)
        {
            Logger.errorslog($"[SetAvatar] Geçersiz avatar ID: {Id} from {session.AccountId}");
            return;
        }
        
        AccountManager.AccountData account = AccountCache.Load(session.AccountId);
        if (account == null)
        {
            Logger.errorslog($"[SetAvatar] Account bulunamadı: {session.AccountId}");
            return;
        }
        account.Avatarid = Id;
        Console.WriteLine("Avatar değiştirildi: " + Id);

    }
}