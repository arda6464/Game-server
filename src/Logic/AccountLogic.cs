using System;
using System.Collections.Generic;
using System.Linq;

namespace Logic
{
    public class AccountLogic
    {
        public AccountManager.AccountData Data { get; private set; }
        public Session Session { get; private set; }

        public AccountLogic(AccountManager.AccountData data, Session session)
        {
            Data = data;
            Session = session;
        }

        /// <summary>
        /// Oyuncu ID'sine göre AccountLogic döner. Hesap yoksa null döner.
        /// </summary>
        public static AccountLogic Get(int accountId)
        {
            var data = AccountCache.Load(accountId);
            if (data == null) return null;

            var session = SessionManager.GetSession(accountId);
            return session?.Logic ?? new AccountLogic(data, session);
        }

        /// <summary>
        /// Oyuncu lobiye girdiğinde tetiklenen işlemler (görev yenileme vb.)
        /// </summary>
        public void HomeVisited()
        {
            if (Data == null) return;

            // Statik manager'dan mantığı buraya taşıyacağız
            QuestManager.CheckAndRefreshQuests(Data);

            Console.WriteLine($"[AccountLogic] HomeVisited tetiklendi: {Data.ID}");
        }

        /// <summary>
        /// Oyuncuya yeni bir bildirim ekler ve online ise gönderir.
        /// </summary>
        public void AddNotification(Notfication notification)
        {
            if (Data == null) return;

            if (notification.type == NotficationTypes.NotficationType.Push)
            {
                AndroidNotficationManager.SendNotification(notification.Title, notification.Message, Data.FBNToken);
                return;
            }

            if (Session != null)
            {
                NotficationSender.Send(Session, notification);
            }

            lock (Data.SyncLock)
            {
                Data.Notfications.Add(notification);
            }

            Logger.genellog($"{Data.Username} kullanıcısına bildirim eklendi: {notification.Title}");
        }

        /// <summary>
        /// Oyuncuya yeni bir görev ekler.
        /// </summary>
        public void AddQuest(Quest quest)
        {
            if (Data == null || quest == null) return;

            lock (Data.SyncLock)
            {
                Data.Quests.Add(quest);
            }
            // Yeni görev eklendiğinde istemciye bildirilebilir
        }

        /// <summary>
        /// Bir görevi siler ve istemciye bildirir.
        /// </summary>
        public void RemoveQuest(Quest quest)
        {
            if (Data == null || quest == null) return;

            lock (Data.SyncLock)
            {
                if (!Data.Quests.Remove(quest)) return;
            }

            if (Session != null && Session.IsConnected)
            {
                using (var buffer = new ByteBuffer())
                {
                    buffer.WriteVarInt((int)MessageType.DeleteQuest);
                    buffer.WriteByte((byte)quest.ID);
                    Session.Send(buffer.ToArray());
                }
            }
        }

        /// <summary>
        /// Görev ilerlemesini kontrol eder ve tamamlandığında ödülü verir.
        /// </summary>
        public void UpdateQuestProgress(Quest.MissionType type, int amount = 1)
        {
            if (Data == null) return;

            List<Quest> updatedQuests = new List<Quest>();

            lock (Data.SyncLock)
            {
                var matchingQuests = Data.Quests.Where(q => q.Type == type && !q.IsCompleted).ToList();
                foreach (var quest in matchingQuests)
                {
                    quest.CurrentGoal += amount;
                    if (quest.CurrentGoal >= quest.Target)
                    {
                        quest.CurrentGoal = quest.Target;
                        quest.IsCompleted = true;

                        // Ödül verme mantığı (Gems)
                        Data.Gems += quest.Goal;
                        Logger.genellog($"[QUEST COMPLETED] {Data.Username} görevi tamamladı: {quest.ID} - Ödül: {quest.Goal} Gems");
                    }
                    updatedQuests.Add(quest);
                }
            }

            if (updatedQuests.Count > 0 && Session != null && Session.IsConnected)
            {
                using (var buffer = new ByteBuffer())
                {
                    buffer.WriteVarInt((int)MessageType.QuestProgress);
                    buffer.WriteVarInt(updatedQuests.Count);
                    foreach (var quest in updatedQuests)
                    {
                        buffer.WriteByte((byte)quest.ID);
                        buffer.WriteVarInt(quest.CurrentGoal);
                        buffer.WriteBool(quest.IsCompleted);
                    }
                    Session.Send(buffer.ToArray());
                }
            }

            if (updatedQuests.Count > 0)
            {
                AccountManager.SaveAccounts();
            }
        }

        /// <summary>
        /// Tüm görevleri istemciye senkronize eder.
        /// </summary>
        public void SyncQuests()
        {
            if (Session == null || !Session.IsConnected) return;

            List<Quest> questsCopy;
            lock (Data.SyncLock)
            {
                questsCopy = new List<Quest>(Data.Quests);
            }

            using (ByteBuffer buffer = new ByteBuffer())
            {
                buffer.WriteVarInt((int)MessageType.NewQuest);
                buffer.WriteVarLong(QuestManager.GetNextQuestRefreshTime());
                buffer.WriteVarLong(QuestManager.GetNextSeasonalQuestRefreshTime());
                buffer.WriteVarInt(questsCopy.Count);

                foreach (var quest in questsCopy)
                {
                    buffer.WriteByte((byte)quest.ID);
                    buffer.WriteByte((byte)quest.Type);
                    buffer.WriteVarInt(quest.Target);
                    buffer.WriteVarInt(quest.CurrentGoal);
                    buffer.WriteBool(quest.IsCompleted);
                    buffer.WriteByte((byte)quest.RewardType);
                    buffer.WriteVarInt(quest.Goal);
                    buffer.WriteBool(quest.IsPremium);
                    buffer.WriteBool(quest.IsDailyQuest);
                }

                Session.Send(buffer.ToArray());
            }
        }

        /// <summary>
        /// Oyuncunun premium durumunu günceller.
        /// </summary>
        public void SetPremium(int level, DateTime? endTime = null)
        {
            if (Data == null) return;

            Data.Premium = level;
            if (endTime.HasValue)
            {
                Data.PremiumEndTime = endTime.Value;
            }

            Logger.genellog($"{Data.Username} premium durumu güncellendi: {level}");
            // AccountManager.SaveAccounts(); // Veriyi kaydet
        }

        public void SetNameColor(int colorId)
        {
            if (Data == null) return;

            // Color ID validasyonu (1-15 arası)
            if (colorId < 1 || colorId > 15)
            {
                Logger.errorslog($"[SetColor] Geçersiz color ID: {colorId} from {Data.ID}");
                return;
            }

            Data.Namecolorid = colorId;
            Console.WriteLine($"[AccountLogic] Name Color değiştirildi: {colorId} ({Data.Username})");

            // Kulüp verisini güncelle
            if (Data.Clubid != -1) ClubManager.MemberDataUpdate(Data.ID, Data.Clubid);

            AccountManager.SaveAccounts();
        }

        /// <summary>
        /// Oyuncunun istatistiklerini (Level, Gems, Coins, Trophy) günceller.
        /// </summary>
        public void UpdateStats(int? level = null, int? gems = null, int? coins = null, int? trophies = null)
        {
            if (Data == null) return;

            bool changed = false;
            if (level.HasValue) { Data.Level = level.Value; changed = true; }
            if (gems.HasValue) { Data.Gems = gems.Value; changed = true; }
            if (coins.HasValue) { Data.Coins = coins.Value; changed = true; }
            if (trophies.HasValue) { Data.Trophy = trophies.Value; changed = true; }

            if (changed)
            {
                Logger.genellog($"{Data.Username} istatistikleri güncellendi (Admin Panel).");
                AccountManager.SaveAccounts();
                SendUpdate(); // Online ise veriyi gönder
            }
        }

        /// <summary>
        /// Oyuncunun ismini değiştirir.
        /// </summary>
        public bool ChangeName(string newName)
        {
            if (Data == null) return false;

            // İsim validasyonu
            if (string.IsNullOrWhiteSpace(newName) || newName.Length < 3 || newName.Length > 20)
            {
                if (Session != null) MessageCodeManager.Send(Session, MessageCodeManager.Message.İnvalidName);
                return false;
            }

            // Yasaklı kelime kontrolü
            string[] bannedWords = { "admin", "moderator", "null", "undefined" };
            if (bannedWords.Any(word => newName.ToLower().Contains(word.ToLower())))
            {
                if (Session != null)
                {
                    using (ByteBuffer buffer = new ByteBuffer())
                    {
                        buffer.WriteVarInt((int)MessageType.NameNotAcceptedRequest);
                        Session.Send(buffer.ToArray());
                    }
                }
                return false;
            }

            string oldName = Data.Username;
            Data.Username = newName;

            if (Session != null && Session.IsConnected)
            {
                var response = new ChangeNameResponsePacket { NewName = newName };
                Session.Send(response);
            }

            Logger.genellog($"{oldName} -> {newName} (İsim değiştirildi)");

            // Kulüp verisini güncelle
            if (Data.Clubid != -1) ClubManager.MemberDataUpdate(Data.ID, Data.Clubid);

            AccountManager.SaveAccounts();
            return true;
        }
        /// <summary>
        /// Oyuncuyu belirtilen süre boyunca susturur.
        /// </summary>

        public void Mute(TimeSpan duration)
        {
            if (Data == null) return;

            lock (Data.SyncLock)
            {
                Data.Muted = true;
                Data.MutedEndTime = DateTime.Now.Add(duration);
            }

            Logger.genellog($"{Data.Username} ({Data.ID}) susturuldu. Bitiş: {Data.MutedEndTime}");


            // Eğer online ise bilgilendir veya paket gönder (Opsiyonel)
            SendUpdate();
        }

        /// <summary>
        /// Oyuncunun susturmasını kaldırır.
        /// </summary>
        public void Unmute()
        {
            if (Data == null) return;

            lock (Data.SyncLock)
            {
                Data.Muted = false;
                Data.MutedEndTime = DateTime.MinValue;
            }

            Logger.genellog($"{Data.Username} ({Data.ID}) susturması kaldırıldı.");

            SendUpdate();
        }

        /// <summary>
        /// Oyuncunun şu an susturulmuş olup olmadığını kontrol eder (Süre kontrolü dahil).
        /// </summary>
        public bool IsMuted()
        {
            if (Data == null || !Data.Muted) return false;

            // Süre sınırsız değilse (MinValue/MaxValue kontrolü yapılabilir) ve süre dolmuşsa
            if (Data.MutedEndTime != DateTime.MinValue && DateTime.Now > Data.MutedEndTime)
            {
                Unmute(); // Süre dolduğu için susturmayı kaldır
                return false;
            }

            return true;
        }

        /// <summary>
        /// Oyuncunun avatarını değiştirir.
        /// </summary>
        public bool SetAvatar(int avatarId)
        {
            if (Data == null) return false;

            // Avatar ID validasyonu (1-10 arası)
            if (avatarId < 1 || avatarId > 10)
            {
                if (Session != null) MessageCodeManager.Send(Session, MessageCodeManager.Message.İnvalidAvatar);
                return false;
            }

            Data.Avatarid = avatarId;
            Console.WriteLine($"[AccountLogic] Avatar değiştirildi: {avatarId} ({Data.Username})");

            // Kulüp verisini güncelle
            if (Data.Clubid != -1) ClubManager.MemberDataUpdate(Data.ID, Data.Clubid);

            AccountManager.SaveAccounts();
            return true;
        }

        /// <summary>
        /// Güncel hesap verilerini kullanıcıya gönderir (Online ise).
        /// </summary>
        public void SendUpdate()
        {
            if (Session != null && Session.IsConnected)
            {
                // AccountData paketi gönder
                Session.Send(new AccountDataPacket(Data));
            }
        }


        public void AddRole(Role.Roles role)
        {
            if (Data == null) return;

            lock (Data.SyncLock)
            {
                if (Data.Roles.Contains(role))
                {
                    Logger.genellog($"{Data.Username} ({Data.ID}) kişisine {role} eklenmeye çalıştı fakat zaten var olduğu için eklenmedi");
                    return;
                }
                Data.Roles.Add(role);
            }
            Logger.genellog($"{Data.Username} ({Data.ID}) kişisine {role} eklendi!");
            AccountManager.SaveAccounts();
        }

        public void RemoveRole(Role.Roles role)
        {
            if (Data == null) return;

            lock (Data.SyncLock)
            {
                if (!Data.Roles.Contains(role))
                {
                    Logger.genellog($"{Data.Username} ({Data.ID}) kişisinden {role} kaldırılmaya çalıştı fakat zaten o role sahip olmadığı için kaldırılmadı");
                    return;
                }
                Data.Roles.Remove(role);
            }
            Logger.genellog($"{Data.Username} ({Data.ID}) kişisinden {role} kaldırıldı!");
            AccountManager.SaveAccounts();
        }

        /// <summary>
        /// Oyuncunun premium durumunu kaldırır.
        /// </summary>
        public void RemovePremium()
        {
            if (Data == null) return;

            if (Data.Premium > 0)
            {
                Data.Premium = 0;
                Logger.genellog($"{Data.Username} premium kaldırıldı.");
                // AccountManager.SaveAccounts();
            }
        }
    }
}
