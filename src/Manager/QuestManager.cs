using static Quest;

public static class QuestManager
{
    private static readonly Random _random = new();
    public static Quest GetQuestById(AccountManager.AccountData account, int id)
    {
        lock (account.SyncLock)
        {
            return account.Quests.Find(q => q.ID == id);
        }
    }

    public static List<Quest> GetAllQuests(AccountManager.AccountData account)
    {
        lock (account.SyncLock)
        {
            return new List<Quest>(account.Quests);
        }
    }

    public static void AddRandomQuest(AccountManager.AccountData account,bool IsSeason)
    {
        lock (account.SyncLock)
        {
            // Yeni quest'ler ekle
            AddQuest1000(false, account);
            AddQuest1000(false, account);
            AddQuest1000(false, account);
            AddQuest2500(true, true, account);
            if(IsSeason)
            {
                AddQuest5000(false, account);
                AddQuest5000(true, account);
                AddQuest10000(false, account);
            }
        }
    }

    public static void AddQuest2500(bool isDailyQuest, bool isPremium, AccountManager.AccountData account)
    {
        AddQuest(account, 2500, _random.Next(3, 6), isDailyQuest, isPremium);
    }

    public static void AddQuest5000(bool isPremium, AccountManager.AccountData account)
    {
        AddQuest(account, 5000, _random.Next(5, 11), false, isPremium);
    }

    public static void AddQuest10000(bool isPremium, AccountManager.AccountData account)
    {
        AddQuest(account, 10000, _random.Next(10, 16), false, isPremium);
    }

    public static void AddQuest1000(bool isPremium, AccountManager.AccountData account)
    {
        AddQuest(account, 1000, 3, true, isPremium);
    }

    private static void AddQuest(AccountManager.AccountData account, int reward, int target, bool isDailyQuest, bool isPremium)
    {
        lock (account.SyncLock)
        {
            // Benzersiz ID oluştur
            int questId;
            var usedIds = new HashSet<int>(account.Quests.Select(q => q.ID));
            
            do
            {
                questId = _random.Next(0, 256);
            } while (usedIds.Contains(questId));

            // Rastgele mission tipi
            var missionTypeCount = Enum.GetValues(typeof(MissionType)).Length;
            var missionType = (MissionType)_random.Next(0, missionTypeCount);

            var newQuest = new Quest
            {
                ID = questId,
                Type = missionType,
                CurrentGoal = 0,
                IsCompleted = false,
                IsDailyQuest = isDailyQuest,
                IsPremium = isPremium,
                RewardType = ItemType.Gems,
                Goal = reward,
                Target = target
            };

            account.Quests.Add(newQuest);
        }
    }

    public static void CheckAndRefreshQuests(AccountManager.AccountData account)
    {
        var now = DateTime.Now;
        int refreshHour = Config.Instance.QuestRefeshHour;
        
        // Bugünün yenilenme zamanı
        DateTime todayRefreshTime = now.Date.AddHours(refreshHour);
        
        lock (account.SyncLock)
        {
            // Eğer şu an yenilenme saatinden sonraysak ve son yenilenme bu saatin öncesindeyse -> YENİLE
            if (now >= todayRefreshTime && account.LastQuestRefreshDate < todayRefreshTime)
            {
                // Sezonluk görev kontrolü
                bool giveSeasonal = Config.Instance.SeasonQuestRefeshTime <= now;
                if (giveSeasonal)
                {
                    Config.Instance.SeasonQuestRefeshTime = now.Date.AddDays(2).AddHours(refreshHour);
                }

                AddRandomQuest(account, giveSeasonal);
                account.LastQuestRefreshDate = now;
                
                Console.WriteLine($"[Quest] {account.Username} için görevler yenilendi. Sezonluk: {giveSeasonal}");
            }
        }
    }

    public static void SendQuest(AccountManager.AccountData account)
    {
        if (!SessionManager.IsOnline(account.AccountId))
            return;

        var session = SessionManager.GetSession(account.AccountId);
        if (session == null)
            return;

        List<Quest> questsCopy;
        lock (account.SyncLock)
        {
            questsCopy = new List<Quest>(account.Quests);
        }

        using (ByteBuffer buffer = new ByteBuffer())
        {
            buffer.WriteInt((int)MessageType.NewQuest);
            buffer.WriteLong(GetNextQuestRefreshTime());
            buffer.WriteByte((byte)questsCopy.Count);

            foreach (var quest in questsCopy)
            {
                buffer.WriteByte((byte)quest.ID);
                buffer.WriteByte((byte)quest.Type);
                buffer.WriteShort((short)quest.Target);
                buffer.WriteByte((byte)quest.CurrentGoal);
                buffer.WriteBool(quest.IsCompleted);
                buffer.WriteByte((byte)quest.RewardType);
                buffer.WriteShort((short)quest.Goal);
                buffer.WriteBool(quest.IsPremium);
                buffer.WriteBool(quest.IsDailyQuest);
            }

            session.Send(buffer.ToArray());
        }
    }
    public static long GetNextQuestRefreshTime()
    {
        DateTime nowUtc = DateTime.UtcNow;
        DateTime todayUtc = nowUtc.Date;

        int refreshHour = Config.Instance.QuestRefeshHour;
        DateTime refreshTimeUtc = todayUtc.AddHours(refreshHour);
        
        if (nowUtc >= refreshTimeUtc)
        {
            refreshTimeUtc = refreshTimeUtc.AddDays(1);
        }

        DateTimeOffset offset = new DateTimeOffset(refreshTimeUtc, TimeSpan.Zero);
        return offset.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Sezonluk görevlerin bir sonraki yenilenme zamanını döndürür (Unix timestamp)
    /// </summary>
    public static long GetNextSeasonalQuestRefreshTime()
    {
        // Config'deki SeasonQuestRefeshTime zaten bir sonraki tarihi tutuyor
        DateTime seasonRefreshTime = Config.Instance.SeasonQuestRefeshTime;
        
        // UTC'ye çevir
        DateTime seasonRefreshUtc = seasonRefreshTime.ToUniversalTime();
        
        DateTimeOffset offset = new DateTimeOffset(seasonRefreshUtc, TimeSpan.Zero);
        return offset.ToUnixTimeSeconds();
    }

    public static void DeleteQuest(AccountManager.AccountData account, Quest quest)
    {
        lock (account.SyncLock)
        {
            if (!account.Quests.Remove(quest))
                return;
        }

        if (SessionManager.IsOnline(account.AccountId))
        {
            var session = SessionManager.GetSession(account.AccountId);
            if (session != null)
            {
                using (var buffer = new ByteBuffer())
                {
                    buffer.WriteInt((int)MessageType.DeleteQuest);
                    buffer.WriteByte((byte)quest.ID);
                    session.Send(buffer.ToArray());
                }
            }
        }
    }

    public static void CheckQuestProgress(AccountManager.AccountData account, MissionType type, int amount = 1)
    {
        if (account == null) return;

        List<Quest> updatedQuests = new List<Quest>();

        lock (account.SyncLock)
        {
            // İlgili tipteki ve tamamlanmamış görevleri bul
            var matchingQuests = account.Quests.Where(q => q.Type == type && !q.IsCompleted).ToList();

            foreach (var quest in matchingQuests)
            {
                quest.CurrentGoal += amount;

                // Hedefe ulaşıldı mı?
                if (quest.CurrentGoal >= quest.Target)
                {
                    quest.CurrentGoal = quest.Target;
                    quest.IsCompleted = true;
                    
                    // Ödülü ver (Şimdilik sadece log, ilerde ItemManager/Inventory update eklenebilir)
                    Console.WriteLine($"[QUEST COMPLETED] {account.Username} görevi tamamladı: {quest.ID} - Ödül: {quest.Goal} Gems");
                    
                    // TODO: Ödülü hesaba ekle
                    // account.Gems += quest.Goal; 
                }
                
                updatedQuests.Add(quest);
            }
        }

        // Güncellemeleri client'a bildir
        if (updatedQuests.Count > 0 && SessionManager.IsOnline(account.AccountId))
        {
            var session = SessionManager.GetSession(account.AccountId);
            if (session != null)
            {
                using (var buffer = new ByteBuffer())
                {
                    // QuestProgress paket yapısı: [PaketID] [QuestCount] [QuestID1] [CurrentGoal1] [IsCompleted1] ...
                    buffer.WriteInt((int)MessageType.QuestProgress); 
                    buffer.WriteByte((byte)updatedQuests.Count);

                    foreach (var quest in updatedQuests)
                    {
                        buffer.WriteByte((byte)quest.ID);
                        buffer.WriteInt(quest.CurrentGoal);
                        buffer.WriteBool(quest.IsCompleted);
                    }
                    session.Send(buffer.ToArray());
                }
            }
        }
    }
}
