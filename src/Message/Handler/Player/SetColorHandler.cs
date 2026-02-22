using System;

[PacketHandler(MessageType.ChangeNameColorRequest)]
public static class SetNameColor
{
    public static void Handle(Session session, byte[] data)
    {

        Console.WriteLine("SetCOLOR");
        ByteBuffer BUFFER = new ByteBuffer();
        BUFFER.WriteBytes(data, true);
        int _ = BUFFER.ReadShort();

        var request = new SetNameColorRequestPacket();
        request.Deserialize(BUFFER);

        int Id = request.ColorId;
        BUFFER.Dispose();
        
        // Color ID validasyonu (1-15 arası)
        if (Id < 1 || Id > 15)
        {
            Logger.errorslog($"[SetColor] Geçersiz color ID: {Id} from {session.AccountId}");
            return;
        }
        
        AccountManager.AccountData account = session.Account;
        if (account == null)
        {
            Logger.errorslog($"[SetColor] Account bulunamadı: {session.AccountId}");
            return;
        }
        account.Namecolorid = Id;
        Console.WriteLine("Name Color değiştirildi: " + Id);
         if (account.Clubid != -1) ClubManager.MemberDataUpdate(account.AccountId, account.Clubid);

    }
}