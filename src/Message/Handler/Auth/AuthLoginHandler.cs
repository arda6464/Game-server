using System;
using Logic;

[PacketHandler(MessageType.AuthLoginRequest)]
public static class AuthLoginHandler
{
    public static void Handle(Session session, byte[] data)
    {


        using (ByteBuffer buffer = new ByteBuffer())
        {
            buffer.WriteBytes(data, true);
            
            // 1. İsteği Oku
            var request = new AuthLoginRequestPacket();
            request.Deserialize(buffer);

            Console.WriteLine($"Token: {request.Token} accountıd: {request.ID} Dil: {request.Language}");

            // 2. Kontroller
            if (Config.Instance?.ServerVersion != request.ClientVersion)
            {
                Notfication notification = new Notfication
                {
                    type = NotficationTypes.NotficationType.banner,
                    Title = "Güncelleme mevcut",
                    Message = "oyunumuzu güncelledik yenilikleri görmek için indirin!",
                    ButtonText = "indir",
                    Url = Config.Instance.UpdateLink
                };
                NotficationSender.Send(session, notification);
                session.Close();
                return;
            }

            int accountID = request.ID;

            if (string.IsNullOrWhiteSpace(request.Token) || request.ID == 0)
            {
                Logger.errorslog($"giriş yapmak isteyen kişinin tokeni null... yeni hesap oluşturuluyor");
                AccountManager.AccountData newaccount = AccountManager.CreateAccount(request.Language);
                session.ID = newaccount.ID;

                // Yeni Hesap Bilgisi Gönder
                var newAccPacket = new NewAccountCreateResponsePacket
                {
                    Token = newaccount.Token,
                    ID = newaccount.ID,
                    ConnectionToken = session.ConnectionToken
                };
                session.Send(newAccPacket);

                accountID = newaccount.ID;
            }

            AccountManager.AccountData account = AccountCache.Load(accountID);
            if (account == null)
            {
                Loginfailed.Send(session, "verileri temizleyin, hesap bulunamadı", 1);
                return;
            }
            Console.WriteLine($"merhaba {account.Username} hesabına başarılı şekilde giriş yaptın");

            if (BanManager.IsBanned(account.ID))
            {
                string mesage = BanManager.GetBanMessage(account.ID);
                Loginfailed.Send(session, mesage, 1);
                return;
            }

            session.ID = account.ID;
            session.ID = account.ID;
            session.PlayerData = new Player
            {
                ID = account.ID,
                Username = account.Username,
                AvatarId = account.Avatarid,
                session = session
            };
            SessionManager.AddSession(account.ID, session);
            session.Account = account;
            if (session.FBNToken != null)
            {
                account.FBNToken = session.FBNToken;
                Console.WriteLine($"FBN Token kaydedildi: {account.FBNToken} (AccountID: {account.ID})");
            }

            session.ChangeState(PlayerState.Lobby);
            PlayerSetPresence.Handle(session, PlayerSetPresence.PresenceState.Online);

            // 3. Yanıtı Hazırla (Packet Kullanarak)
            AuthLoginResponsePacket response = new AuthLoginResponsePacket
            {
                Account = account,
                 ConnectionToken = session.ConnectionToken,
                Club = ClubManager.LoadClub(account.Clubid),
                RandomClubs = ClubManager.RandomList(10),
                NextQuestRefreshTime = QuestManager.GetNextQuestRefreshTime(),
                NextSeasonalQuestRefreshTime = QuestManager.GetNextSeasonalQuestRefreshTime()
            };

            session.Send(response);


            #region notifications
            lock (account.SyncLock)
            {
                foreach (var inboxnotification in account.inboxesNotfications)
                {
                    NotficationSender.Send(session, inboxnotification);
                    System.Threading.Thread.Sleep(50);
                }
                foreach (var notficaiton in account.Notfications)
                {
                    if (!notficaiton.IsViewed)
                    {
                        NotficationSender.Send(session, notficaiton);
                        notficaiton.IsViewed = true;
                    }

                    System.Threading.Thread.Sleep(50);
                }
            }
            #endregion
        }







        #region notifications
        #region notifications
        /* lock (account.SyncLock)
         {
             foreach (var inboxnotification in account.inboxesNotfications)
             {
                 NotficationSender.Send(session, inboxnotification);
                 System.Threading.Thread.Sleep(50);
             }
             foreach (var notficaiton in account.Notfications)
             {
                 if (!notficaiton.IsViewed)
                 {
                     NotficationSender.Send(session, notficaiton);
                     notficaiton.IsViewed = true;
                 }

                 System.Threading.Thread.Sleep(50);
             }
         }*/
        #endregion







    }



}
        #endregion
