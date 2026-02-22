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
            int _ = buffer.ReadShort(); // Packet ID atla

            // 1. İsteği Oku
            var request = new AuthLoginRequestPacket();
            request.Deserialize(buffer);

            Console.WriteLine($"Token: {request.Token} accountıd: {request.AccountId} Dil: {request.Language}");

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

            string accountID = request.AccountId;

            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.AccountId))
            {
                Logger.errorslog($"giriş yapmak isteyen kişinin tokeni null... yeni hesap oluşturuluyor");
                AccountManager.AccountData newaccount = AccountManager.CreateAccount(request.Language);
                session.AccountId = newaccount.AccountId;

                // Yeni Hesap Bilgisi Gönder
                var newAccPacket = new NewAccountCreateResponsePacket
                {
                    Token = newaccount.Token,
                    AccountId = newaccount.AccountId
                };
                session.Send(newAccPacket);

                accountID = newaccount.AccountId;
            }

            AccountManager.AccountData account = AccountCache.Load(accountID);
            if (account == null)
            {
                Loginfailed.Send(session, "verileri temizleyin, hesap bulunamadı", 1);
                return;
            }
            Console.WriteLine($"merhaba {account.Username} hesabına başarılı şekilde giriş yaptın");

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
            session.Account = account;
            if (session.FBNToken != null)
            {
                account.FBNToken = session.FBNToken;
                Console.WriteLine($"FBN Token kaydedildi: {account.FBNToken} (AccountID: {account.AccountId})");
            }

            session.ChangeState(PlayerState.Lobby);
            PlayerSetPresence.Handle(session, PlayerSetPresence.PresenceState.Online);

            // 3. Yanıtı Hazırla (Packet Kullanarak)
            var response = new AuthLoginResponsePacket
            {
                Account = account,

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