public static class ChangeNameHandler
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        int _ = read.ReadInt();

        string newname = read.ReadString();
        read.Dispose();

        AccountManager.AccountData account = AccountCache.Load(session.AccountId);
        if (account == null) return;
        
        // Aynı isim kontrolü
        if (account.Username == newname) 
        {
            Logger.errorslog($"[ChangeNameHandler] Aynı isim tekrar kullanılamaz: {newname}");
            return;
        }
        
        // İsim validasyonu
        if (string.IsNullOrWhiteSpace(newname) || newname.Length < 3 || newname.Length > 20)
        {
            Logger.errorslog($"[ChangeNameHandler] Geçersiz isim uzunluğu: {newname}");
            return;
        }
        
        // Yasaklı kelime kontrolü (basit)
        string[] bannedWords = { "admin", "moderator", "admin", "null", "undefined" };
        if (bannedWords.Any(word => newname.ToLower().Contains(word.ToLower())))
        {
            Logger.errorslog($"[ChangeNameHandler] Yasaklı kelime içeren isim: {newname}");
            return;
        }
        
        string oldname = account.Username;
        account.Username = newname;
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.ChangeNameResponse);

        buffer.WriteString(newname);
        byte[] data = buffer.ToArray();
        buffer.Dispose();
        session.Send(data);
        Logger.genellog($"{oldname} adlı kullanıcının adı başarılı şekilde değişti. yeni ismi: {account.Username}");

        if (account.Clubid != -1) ClubManager.MemberDataUpdate(account.AccountId, account.Clubid);
    }
}