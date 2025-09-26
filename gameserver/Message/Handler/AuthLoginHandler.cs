using System;

public static class AuthLoginHandler
{
    public static void Handle(Session session, byte[] data)
    {
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteBytes(data, true);
        int _ = buffer.ReadInt();


        //read
        string token = buffer.ReadString();
        string accountID = buffer.ReadString();
        string Dil = buffer.ReadString();
        buffer.Dispose();


        ByteBuffer byteBuffer = new ByteBuffer();
        // kontrol
        if (token == null || token == " ")
        {
            Logger.errorslog($"giriş yapmak isteyen kişinin tokeni null... yeni hesap oluşturuluyor");
            AccountManager.AccountData newaccount = AccountManager.CreateAccount(Dil);
            byteBuffer.WriteInt((int)MessageType.AuthLoginResponse); // response clientte gerçekleştirilcek unutma orda
            byteBuffer.WriteString(newaccount.Token);
            byteBuffer.WriteString(newaccount.AccountId); // clientte veriler  kaydedilcek, sonra clientin tekrar başlatılması istenecek
            byte[] gonderilcekveri = buffer.ToArray();
            byteBuffer.Dispose();
            session.Send(gonderilcekveri);
            return;
        }
        AccountManager.AccountData account = AccountCache.Load(accountID);
        if (account == null)
        {
            Loginfailed.Send(session, "verileri temizleyin", 1);
            return;
        }
        Console.WriteLine($"merhaba {account.Username} hesabına başarılı şekilde giriş yaptın");

        if (account.Token != token)
        {
            Console.WriteLine("tokenler eşleşmiyor");
            account.Banned = true;

            Loginfailed.Send(session, "Sıra dışı veriler tespit edildi", 1);
            return;
        }

        if (account.Banned)
        {
            Loginfailed.Send(session, "Hesabınız banlandı", 1);
            return;
        }
        // todo accountdata      
        byteBuffer.WriteString(account.Username);

        byteBuffer.WriteInt(account.Avatarid);

        byteBuffer.WriteInt(account.Namecolorid);

        byteBuffer.WriteInt(account.Level);

        byteBuffer.WriteInt(account.Clubid);

        byteBuffer.WriteInt(account.Premium);

        // ilk clubcount yazılcak
        // todo: var randomclub

        byteBuffer.WriteString("kulüpte değil");
        byteBuffer.WriteString("açıklama");
        byteBuffer.WriteInt(1); // club kupa
        byteBuffer.WriteInt(0); // kulüp kişi sayısı

        // friends and request

        byteBuffer.WriteInt(account.Friends.Count); 

        foreach (var friend in account.Friends)
        {
            byteBuffer.WriteString(friend.Id);
            byteBuffer.WriteInt(friend.AvatarId);
            byteBuffer.WriteString(friend.Username);
        }
        byteBuffer.WriteInt(account.Requests.Count);
        foreach (var request in account.Requests)
        {
            byteBuffer.WriteString(request.Id);
            byteBuffer.WriteInt(request.AvatarId);
            byteBuffer.WriteString(request.Username);
        }
        byte[] accountdata = byteBuffer.ToArray();
        byteBuffer.Dispose();
        session.Send(accountdata);



    }
}
