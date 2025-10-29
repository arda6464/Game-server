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

        /*if (account.Token != token)
        {
            Console.WriteLine("tokenler eşleşmiyor");
            account.Banned = true;

            Loginfailed.Send(session, "Sıra dışı veriler tespit edildi", 1);
            return;
        }*/

        if (account.Banned)
        {
            Loginfailed.Send(session, "Hesabınız banlandı", 1);
            return;
        }
        session.AccountId = account.AccountId;
        
        session.PlayerData = new Player
        {
            AccountId = account.AccountId,
            Username = account.Username,
            AvatarId = account.Avatarid,
            session = session
        };
        SessionManager.AddSession(account.AccountId, session);
        byteBuffer.WriteInt((int)MessageType.AuthLoginResponse);
        // accountdata  
        byteBuffer.WriteString(account.AccountId);    
        byteBuffer.WriteString(account.Username);

        byteBuffer.WriteInt(account.Avatarid);

        byteBuffer.WriteInt(account.Namecolorid);

        byteBuffer.WriteInt(account.Level);

        byteBuffer.WriteInt(account.Clubid);

        byteBuffer.WriteInt(account.Premium);




       var club = ClubManager.LoadClub(account.Clubid);

        if (club == null)
        {
            // Club null ise default değerler yaz
            byteBuffer.WriteInt(-1); // ClubId
            byteBuffer.WriteString("kulüpte değil");
            byteBuffer.WriteString("açıklama");
            byteBuffer.WriteInt(1); // TotalKupa
            byteBuffer.WriteInt(0); // Members.Count
            byteBuffer.WriteInt(0);
        }
        else
        {
            // Club null değilse normal değerleri yaz
            byteBuffer.WriteInt(club.ClubId);
            byteBuffer.WriteString(club.ClubName ?? "kulüpte değil");
            byteBuffer.WriteString(club.Clubaciklama ?? "açıklama");
            byteBuffer.WriteInt(club.TotalKupa ?? 1);
            byteBuffer.WriteInt(club.Members.Count);
            byteBuffer.WriteInt(club.Messages.Count);
        }

        
       foreach (var message in (club?.Messages ?? new List<ClubMessage>()))
        {
            byteBuffer.WriteString(message.SenderId);
                byteBuffer.WriteString(message.SenderName);
               byteBuffer.WriteInt(message.SenderAvatarID);
                byteBuffer.WriteString("Üye");
               byteBuffer.WriteString(message.Content);
        }

        var randomclubs = ClubManager.RandomList(10);
        byteBuffer.WriteInt(randomclubs.Count);

        foreach (var rclub in randomclubs)
        {
            byteBuffer.WriteInt(rclub.ClubId);
            byteBuffer.WriteString(rclub.ClubName);
            byteBuffer.WriteString(rclub.Clubaciklama);
            byteBuffer.WriteInt(rclub.TotalKupa ?? 0);
            byteBuffer.WriteInt(rclub.Members.Count);     
            
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
