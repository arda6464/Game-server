[PacketHandler(MessageType.ChangeNameRequest)]
public static class ChangeNameHandler
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        int _ = read.ReadShort();

        var request = new ChangeNameRequestPacket();
        request.Deserialize(read);
        
        string newname = request.NewName;
        read.Dispose();

        AccountManager.AccountData account = session.Account;
        if (account == null) return;
        
        
        
        // İsim validasyonu
        if (string.IsNullOrWhiteSpace(newname) || newname.Length < 3 || newname.Length > 20)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.İnvalidName);
            return;
        }
                                                                                            // todo: birleştir
        // Yasaklı kelime kontrolü (basit)
        string[] bannedWords = { "admin", "moderator", "admin", "null", "undefined" };
        if (bannedWords.Any(word => newname.ToLower().Contains(word.ToLower())))
        {
            using(ByteBuffer buffer = new ByteBuffer()) {
                buffer.WriteShort((short)MessageType.NameNotAcceptedRequest);
                session.Send(buffer.ToArray());
            }
            return;
        }
        
        string oldname = account.Username;
        account.Username = newname;
        
        var response = new ChangeNameResponsePacket { NewName = newname };
        session.Send(response);
        Logger.genellog($"{oldname} adlı kullanıcının adı başarılı şekilde değişti. yeni ismi: {account.Username}");

        if (account.Clubid != -1) ClubManager.MemberDataUpdate(account.AccountId, account.Clubid);
    }
}