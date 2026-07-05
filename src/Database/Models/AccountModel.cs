using System.Text.Json.Serialization;

public class AccountData
    {
        // security data
        public int ID { get; set; }
        public string? Token { get; set; }
        public string? FBNToken { get; set; }

        // account data
        public string? Username { get; set; }
        public int Trophy { get; set; }
        public DateTime CreatedAt;

        public int Premium { get; set; }
        public DateTime PremiumEndTime { get; set; }
        public bool Banned { get; set; }
        public bool Muted { get; set; }
        public bool LookingForTeam { get; set; }
        public bool MuteTeamInvites { get; set; }
        public DateTime MuteTeamInviteEndTime { get; set; }
        public bool DoNotDisturb { get; set; } // rahatsız etme modu
        public DateTime MutedEndTime { get; set; }
        public string? Banreason { get; set; }
        public int Avatarid { get; set; }
        public int Namecolorid { get; set; }
        public int Level { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public int Experience { get; set; }
        public int Gems { get; set; }
        public int Coins { get; set; }
        public int SeasonId { get; set; }
        public int SeasonStartTrophy { get; set; }
        public int SeasonPeakTrophy { get; set; }
        public int SeasonWins { get; set; }
        public int SeasonLosses { get; set; }
        public int SeasonMatchesPlayed { get; set; }
        public bool SeasonRewardClaimed { get; set; }
        public List<SeasonHistoryEntry> SeasonsData { get; set; } = new List<SeasonHistoryEntry>();
        public string? ClubName { get; set; }
        public int Clubid { get; set; }
        public ClubRole clubRole { get; set; }
        public bool TicketBan { get; set; } = false;
        public bool ChatBan { get; set; } = false;
        public bool SendOnlineBestFriendNotification { get; set; } = true;
        public bool SendNewEventNotification { get; set; } = true;
        public bool SendInviteNotification { get; set; } = true;
        public bool SendClaimRewardNotification { get; set; } = true;
        public int WinStreak { get; set; }

        // Günlük giriş ödülü
        public DateTime LastDailyRewardDate { get; set; } = DateTime.MinValue;
        public int DailyRewardStreak { get; set; } = 0;
        public DailyStreakData[] DailyStreakWindow = new DailyStreakData[8];

        // Market / Satın alma verileri
        public int TotalPurchases { get; set; } = 0;
        public DateTime LastPurchaseDate { get; set; } = DateTime.MinValue;
        public List<int> OwnedItems { get; set; } = new List<int>(); // Sahip olunan tekil ürünler (Avatar, NameColor vb.)

        public List<FriendInfo> Friends { get; set; } = new List<FriendInfo>();
        public List<FriendInfo> Requests { get; set; } = new List<FriendInfo>();
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public List<Notification> inboxesNotifications { get; set; } = new List<Notification>();
        public List<Role.Roles> Roles { get; set; } = new List<Role.Roles>();
        public List<SupportTicketData> Tickets { get; set; } = new List<SupportTicketData>();
        public List<Quest> Quests { get; set; } = new List<Quest>();
        public List<BanData> BanHistory { get; set; } = new List<BanData>();
        public Dictionary<int, DateTime> NotificationCooldowns { get; set; } = new Dictionary<int, DateTime>();

        // login data
        public DateTime LastLogin { get; set; }
        public string? LastIp { get; set; }
        public string? Device { get; set; }
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public DateTime LastQuestRefreshDate { get; set; } = DateTime.MinValue;

        [JsonIgnore]
        public object SyncLock = new object();
    }