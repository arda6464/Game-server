using System;

public static class AuthLoginHandler
{
    public static void Handle(Session session, byte[] data)
    {
       

        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteBytes(data, true);
        int _ = buffer.ReadInt();



        //read
        string ClientVersion = buffer.ReadString();
        string token = buffer.ReadString();
        string accountID = buffer.ReadString();
        string Dil = buffer.ReadString();

        buffer.Dispose();
        Console.WriteLine($"Token: {token} accountıd: {accountID} Dil: {Dil}");

        ByteBuffer byteBuffer = new ByteBuffer(3600);
        // kontrol
        if (Config.Instance.ServerVersion != ClientVersion)
        {
            Notfication notification = new Notfication
            {
                Id = 10,
                Title = "Güncelleme mevcut",
                Message = "oyunumuzu güncelledik yenilikleri görmek için indirin!",
                ButtonText = "indir",
                Url = Config.Instance.UpdateLink
            };
            NotficationSender.Send(session, notification);
            session.Close();
            return;
            
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            Logger.errorslog($"giriş yapmak isteyen kişinin tokeni null... yeni hesap oluşturuluyor");
            AccountManager.AccountData newaccount = AccountManager.CreateAccount(Dil);
            session.AccountId = newaccount.AccountId;
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

        if (BanManager.IsBanned(account.AccountId))
        {
            string mesage = BanManager.GetBanMessage(account.AccountId);
            Loginfailed.Send(session, mesage, 1);
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
          PlayerSetPresence.Handle(session, PlayerSetPresence.PresenceState.Online);
        byteBuffer.WriteInt((int)MessageType.AuthLoginResponse);
        // accountdata  
        byteBuffer.WriteString(account.AccountId);    
        byteBuffer.WriteString(account.Username);

        byteBuffer.WriteInt(account.Avatarid);

        byteBuffer.WriteInt(account.Namecolorid);

        byteBuffer.WriteInt(account.Level);

        byteBuffer.WriteInt(account.Clubid);

        byteBuffer.WriteInt(account.Premium);

        byteBuffer.WriteInt(account.Roles.Count);
        foreach(var role in account.Roles)
        {
            byteBuffer.WriteString(role.ToString());
        }

      /*  foreach (var notification in account.inboxesNotfications)
        {

            NotificationSender.İnboxSend(session, notification);
            Console.WriteLine($"inbox gönderildi sender:{notification.Sender} message:{notification.Message} time: {notification.Timespam}");
        }*/

       var club = ClubManager.LoadClub(account.Clubid);

        if (club == null)
        {
            // Club null ise default değerler yaz
            byteBuffer.WriteInt(-1); // ClubId
            byteBuffer.WriteString("");
            byteBuffer.WriteString("");
            byteBuffer.WriteInt(1); // TotalKupa
            byteBuffer.WriteInt(0); // Members.Count
            byteBuffer.WriteInt(0);
        }
        else
        {
            // Club null değilse normal değerleri yaz
            byteBuffer.WriteInt(club.ClubId);
            byteBuffer.WriteString(club.ClubName);
            byteBuffer.WriteString(club.Clubaciklama);
            byteBuffer.WriteInt(club.TotalKupa ?? 0);
            byteBuffer.WriteInt(club.Members.Count);
            byteBuffer.WriteInt(club.Messages.Count);
        }
        foreach (var message in (club?.Messages ?? new List<ClubMessage>()))
        {
            byteBuffer.WriteByte((byte)message.messageFlags);
           switch((ClubMessageFlags)message.messageFlags)
             {
                case ClubMessageFlags.None:
                 byteBuffer.WriteString(message.SenderId);
            byteBuffer.WriteString(message.SenderName);
            byteBuffer.WriteInt(message.SenderAvatarID);
            byteBuffer.WriteString("Üye"); // todo enum send
            byteBuffer.WriteString(message.Content);
                    break;
                case ClubMessageFlags.HasSystem:
                    byteBuffer.WriteInt((int)message.eventType);
                    byteBuffer.WriteString(message.ActorName);
                    byteBuffer.WriteString(message.ActorID ??"");
                    break;
                case ClubMessageFlags.HasTarget:
                 byteBuffer.WriteInt((int)message.eventType);
                    byteBuffer.WriteString(message.ActorName);
                    byteBuffer.WriteString(message.ActorID);
                    byteBuffer.WriteString(message.TargetName);
                    break;
            }
        }
        if (club == null) byteBuffer.WriteInt(-1);
        else byteBuffer.WriteInt(club.Members.Count);
          
       
        foreach(var member in (club?.Members ?? new List<ClubMember>()))
        {
             
            byteBuffer.WriteString(member.Accountid);
            byteBuffer.WriteString(member.AccountName);
            byteBuffer.WriteString(member.Role.ToString());
            byteBuffer.WriteInt(member.NameColorID);
            byteBuffer.WriteInt(member.AvatarID);
            byteBuffer.WriteBool(SessionManager.IsOnline(member.Accountid));
        
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



        foreach (var inboxnotification in account.inboxesNotfications)
        {
            NotficationSender.Send(session, inboxnotification);
            System.Threading.Thread.Sleep(50);
        }
        foreach(var notficaiton in account.Notfications)
        {
            if(!notficaiton.IsViewed)
            {
                NotficationSender.Send(session, notficaiton);
                notficaiton.IsViewed = true;
            }
            
            System.Threading.Thread.Sleep(50);
        }


        
        



    }
}
