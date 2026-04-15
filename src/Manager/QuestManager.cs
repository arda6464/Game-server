using static Quest;

public static class QuestManager
{
    private static readonly Random _random = new();
   

    

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
        var session = SessionManager.GetSession(account.ID);
        session?.Logic.AddQuest(GenerateQuest(2500, _random.Next(3, 6), isDailyQuest, isPremium));
    }

    public static void AddQuest5000(bool isPremium, AccountManager.AccountData account)
    {
        var session = SessionManager.GetSession(account.ID);
        session?.Logic.AddQuest(GenerateQuest(5000, _random.Next(5, 11), false, isPremium));
    }

    public static void AddQuest10000(bool isPremium, AccountManager.AccountData account)
    {
        var session = SessionManager.GetSession(account.ID);
        session?.Logic.AddQuest(GenerateQuest(10000, _random.Next(10, 16), false, isPremium));
    }

    public static void AddQuest1000(bool isPremium, AccountManager.AccountData account)
    {
        var session = SessionManager.GetSession(account.ID);
        session?.Logic.AddQuest(GenerateQuest(1000, 3, true, isPremium));
    }

    public static Quest GenerateQuest(int reward, int target, bool isDailyQuest, bool isPremium)
    {
        // Benzersiz ID oluştur (Şimdilik rastgele, ileride global veya per-player olabilir)
        int questId = _random.Next(0, 256);

        // Rastgele mission tipi
        var missionTypeCount = Enum.GetValues(typeof(MissionType)).Length;
        var missionType = (MissionType)_random.Next(0, missionTypeCount);

        return new Quest
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
        var session = SessionManager.GetSession(account.ID);
        session?.Logic.SyncQuests();
    }

    public static void DeleteQuest(AccountManager.AccountData account, Quest quest)
    {
        var session = SessionManager.GetSession(account.ID);
        session?.Logic.RemoveQuest(quest);
    }

    public static void CheckQuestProgress(AccountManager.AccountData account, Quest.MissionType type, int amount = 1)
    {
        if (account == null) return;
        var session = SessionManager.GetSession(account.ID);
        session?.Logic.UpdateQuestProgress(type, amount);
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
        DateTime seasonRefreshTime = Config.Instance.SeasonQuestRefeshTime;
        DateTime seasonRefreshUtc = seasonRefreshTime.ToUniversalTime();
        
        DateTimeOffset offset = new DateTimeOffset(seasonRefreshUtc, TimeSpan.Zero);
        return offset.ToUnixTimeSeconds();
    }
}
