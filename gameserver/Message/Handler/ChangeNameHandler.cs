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
        if (account.Username == newname) return; // todo eror any name
                                                 // todo: new name is banned name?
        string oldname = account.Username;
        account.Username = newname;
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.ChangeNameResponse);

        buffer.WriteString(newname);
        byte[] data = buffer.ToArray();
        buffer.Dispose();
        session.Send(data);
        Logger.genellog($"{oldname} adlı kullanıcının adı başarılı şekilde değişti. yeni ismi: {account.Username}");
    }
}