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
                // Loginfailed("verileri temizleyin", connection);
                return;
            }
            Console.WriteLine($"merhaba {account.Username} hesabına başarılı şekilde giriş yaptın");
               
               if (account.Token != token)
        {
            Console.WriteLine("tokenler eşleşmiyor");
            account.Banned = true;
          
          //  Loginfailed("Sıra dışı veriler tespit edildi", connection);
            return;
        }

        if (account.Banned)
        {
          //  Loginfailed("Hesabınız banlandı", connection);
            return;
        }
           // todo accountdata       

    }
}