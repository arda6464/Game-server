using System;

public static class SetNameColor
{
    public static void Handle(Session session, byte[] data)
    {

        Console.WriteLine("SetCOLOR");
        ByteBuffer BUFFER = new ByteBuffer();
        BUFFER.WriteBytes(data, true);
        int _ = BUFFER.ReadInt();

        int Id = BUFFER.ReadInt();
        BUFFER.Dispose();
        
        // Color ID validasyonu (1-15 arası)
        if (Id < 1 || Id > 15)
        {
            Logger.errorslog($"[SetColor] Geçersiz color ID: {Id} from {session.AccountId}");
            return;
        }
        
        AccountManager.AccountData account = AccountCache.Load(session.AccountId);
        if (account == null)
        {
            Logger.errorslog($"[SetColor] Account bulunamadı: {session.AccountId}");
            return;
        }
        account.Namecolorid = Id;
        Console.WriteLine("Name Color değiştirildi: " + Id);

    }
}