using System;

public static class AuthLoginHandler
{
    public static void Handle(Session session, byte[] data)
    {
        double ServerVersion = 0.6;


        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteBytes(data, true);
        int _ = buffer.ReadInt();



        //read
        double ClientVersion = buffer.ReadDouble();
        string token = buffer.ReadString();
        string accountID = buffer.ReadString();
        string Dil = buffer.ReadString();

        buffer.Dispose();
        Console.WriteLine($"Token: {token} accountıd: {accountID} Dil: {Dil}");

        ByteBuffer byteBuffer = new ByteBuffer();
        // kontrol
        if (ServerVersion != ClientVersion)
        {
            Notification notification = new Notification(10, "Güncelleme mevcut", "Sana güzel bir haberimiz var! Oyunumuz yeni güncelleme geldi hemen indir!", "https://store.supercell.com/tr/brawlstars", "Güncelle");
            NotificationSender.Send(session, notification);
            return;
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            Logger.errorslog($"giriş yapmak isteyen kişinin tokeni null... yeni hesap oluşturuluyor");
            AccountManager.AccountData newaccount = AccountManager.CreateAccount(Dil);
            byteBuffer.WriteInt((int)MessageType.NewAccountCreateResponse); // response clientte gerçekleştirilcek unutma orda
            byteBuffer.WriteString(newaccount.Token);
            byteBuffer.WriteString(newaccount.AccountId); // clientte veriler  kaydedilcek, sonra clientin tekrar başlatılması istenecek
            byte[] gonderilcekveri = byteBuffer.ToArray();
            byteBuffer.Dispose();
            session.Send(gonderilcekveri);
            return;
        }
        AccountManager.AccountData account = AccountCache.Load(accountID);
        if (account == null)
        {
            Loginfailed.Send(session, "verileri temizleyin, hesap bulunamadı", 1);
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
        session.AccountId = account.AccountId;
        SessionManager.AddSession(account.AccountId, session);
        byteBuffer.WriteInt((int)MessageType.AuthLoginResponse);
        // accountdata      
        byteBuffer.WriteString(account.Username);

        byteBuffer.WriteInt(account.Avatarid);

        byteBuffer.WriteInt(account.Namecolorid);

        byteBuffer.WriteInt(account.Level);

        byteBuffer.WriteInt(account.Clubid);

        byteBuffer.WriteInt(account.Premium);




        // ilk clubcount yazılcak
        
        // kişisel club data
        byteBuffer.WriteString("kulüpte değil");
        byteBuffer.WriteString("açıklama");
        byteBuffer.WriteInt(1); // club kupa
        byteBuffer.WriteInt(0); // kulüp kişi sayısı

        var randomclubs = ClubManager.RandomList(10);
        byteBuffer.WriteInt(randomclubs.Count);

        foreach (var club in randomclubs)
        {
            byteBuffer.WriteInt(club.ClubId);
            byteBuffer.WriteString(club.ClubName);
            byteBuffer.WriteString(club.Clubaciklama);
            byteBuffer.WriteInt(club.TotalKupa ?? 0);
            byteBuffer.WriteInt(club.Members.Count);     
            
        }
        

        // friends and request

        byteBuffer.WriteInt(account.Friends.Count);

        foreach (var friend in account.Friends)
        {
            byteBuffer.WriteString(friend.Id);
            byteBuffer.WriteInt(friend.AvatarId);
            byteBuffer.WriteString(friend.Username);
            byteBuffer.WriteInt(friend.NameColorID);
            byteBuffer.WriteBool(SessionManager.IsOnline(friend.Id));
        }
        byteBuffer.WriteInt(account.Requests.Count);
        foreach (var request in account.Requests)
        {
            byteBuffer.WriteString(request.Id);
            byteBuffer.WriteInt(request.AvatarId);
            byteBuffer.WriteString(request.Username);
        }
        byte[] accountdata = byteBuffer.ToArray();
       
Console.WriteLine("Gönderilen paket boyu: " + accountdata.Length);

        byteBuffer.Dispose();
        session.Send(accountdata);
          // Notfications
        foreach (var notification in account.Notifications)
        {
            if (!notification.IsViewed) // görmediyse
            {
                NotificationSender.Send(session, notification); // 
                notification.IsViewed = true;
                
            }
        }



    }
}
