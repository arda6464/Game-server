using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class SeasonRewardTier
{
    public int MinTrophy { get; set; }
    public int MaxTrophy { get; set; }
    public int Coins { get; set; }
    public int Gems { get; set; }
    public int Chests { get; set; }
    public string? TitleReward { get; set; }
}

public class SeasonHistoryEntry
{
    public int SeasonId { get; set; }
    public string? SeasonName { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int FinalTrophy { get; set; }
    public int PeakTrophy { get; set; }
    public string? RankName { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int MatchesPlayed { get; set; }
    public int RewardCoins { get; set; }
    public int RewardGems { get; set; }
    public int RewardChests { get; set; }
    public string? RewardTitle { get; set; }
    public DateTime ClaimedAtUtc { get; set; }
}

public class SeasonSummary
{
    public int SeasonId { get; set; }
    public string? SeasonName { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public DateTime ClosedAtUtc { get; set; }
    public int PlayerCount { get; set; }
    public int RewardedPlayerCount { get; set; }
    public bool ResetApplied { get; set; }
}

public class SeasonConfig
{
    public int CurrentSeasonId { get; set; } = 1;
    public string SeasonName { get; set; } = "Season 1";
    public bool IsActive { get; set; } = true;
    public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;
    public DateTime EndTimeUtc { get; set; } = DateTime.UtcNow.AddDays(60);
    public bool HardResetTrophies { get; set; } = true;
    public int ResetTrophyTo { get; set; } = 0;
    public List<SeasonRewardTier> RewardTiers { get; set; } = new List<SeasonRewardTier>();
    public List<SeasonSummary> History { get; set; } = new List<SeasonSummary>();
}

public class SeasonPlayerView
{
    public int AccountId { get; set; }
    public string? Username { get; set; }
    public int CurrentTrophy { get; set; }
    public int PeakTrophy { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int MatchesPlayed { get; set; }
    public bool RewardClaimed { get; set; }
    public int SeasonId { get; set; }
    public List<SeasonHistoryEntry> History { get; set; } = new List<SeasonHistoryEntry>();
}

public static class SeasonManager
{
    private static readonly object _lock = new object();
    private static readonly string _filePath = "season_config.json";

    public static SeasonConfig Config { get; private set; } = new SeasonConfig();

    static SeasonManager()
    {
        Load();
    }

    public static void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    Config = JsonConvert.DeserializeObject<SeasonConfig>(json) ?? new SeasonConfig();
                }
                else
                {
                    Config = CreateDefaultConfig();
                    Save();
                }
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[SeasonManager] Load hatası: {ex.Message}");
                Config = CreateDefaultConfig();
            }
        }
    }

    public static void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[SeasonManager] Save hatası: {ex.Message}");
            }
        }
    }

    public static SeasonConfig GetSnapshot()
    {
        lock (_lock)
        {
            return JsonConvert.DeserializeObject<SeasonConfig>(JsonConvert.SerializeObject(Config)) ?? new SeasonConfig();
        }
    }

    public static void UpdateSettings(SeasonConfig incoming)
    {
        if (incoming == null) return;

        lock (_lock)
        {
            Config.SeasonName = string.IsNullOrWhiteSpace(incoming.SeasonName) ? Config.SeasonName : incoming.SeasonName;
            Config.IsActive = incoming.IsActive;
            Config.StartTimeUtc = incoming.StartTimeUtc == default ? Config.StartTimeUtc : incoming.StartTimeUtc;
            Config.EndTimeUtc = incoming.EndTimeUtc == default ? Config.EndTimeUtc : incoming.EndTimeUtc;
            Config.HardResetTrophies = incoming.HardResetTrophies;
            Config.ResetTrophyTo = incoming.ResetTrophyTo;
            if (incoming.RewardTiers != null && incoming.RewardTiers.Count > 0)
                Config.RewardTiers = incoming.RewardTiers;

            Save();
        }
    }

    public static void EnsureAccountSeasonState(AccountManager.AccountData account)
    {
        if (account == null) return;

        lock (account.SyncLock)
        {
            if (account.SeasonId == Config.CurrentSeasonId && account.SeasonId != 0)
                return;

            account.SeasonId = Config.CurrentSeasonId;
            account.SeasonStartTrophy = account.Trophy;
            account.SeasonPeakTrophy = account.Trophy;
            account.SeasonWins = 0;
            account.SeasonLosses = 0;
            account.SeasonMatchesPlayed = 0;
            account.SeasonRewardClaimed = false;
        }
    }

    public static void RecordBattleResult(AccountManager.AccountData account, bool isWin, int placement, int playerCount, int trophyDelta)
    {
        if (account == null) return;

        EnsureAccountSeasonState(account);

        lock (account.SyncLock)
        {
            account.SeasonMatchesPlayed++;
            if (isWin)
                account.SeasonWins++;
            else
                account.SeasonLosses++;

            if (account.Trophy > account.SeasonPeakTrophy)
                account.SeasonPeakTrophy = account.Trophy;

            account.SeasonId = Config.CurrentSeasonId;
        }
    }

    public static List<SeasonHistoryEntry> GetPlayerHistory(int accountId)
    {
        var account = AccountCache.Load(accountId);
        if (account == null)
            return new List<SeasonHistoryEntry>();

        lock (account.SyncLock)
        {
            return account.SeasonsData
                .OrderByDescending(x => x.SeasonId)
                .ToList();
        }
    }

    public static SeasonPlayerView? GetPlayerView(int accountId)
    {
        var account = AccountCache.Load(accountId);
        if (account == null)
            return null;

        lock (account.SyncLock)
        {
            return new SeasonPlayerView
            {
                AccountId = account.ID,
                Username = account.Username,
                CurrentTrophy = account.Trophy,
                PeakTrophy = account.SeasonPeakTrophy,
                Wins = account.SeasonWins,
                Losses = account.SeasonLosses,
                MatchesPlayed = account.SeasonMatchesPlayed,
                RewardClaimed = account.SeasonRewardClaimed,
                SeasonId = account.SeasonId,
                History = account.SeasonsData.OrderByDescending(x => x.SeasonId).ToList()
            };
        }
    }

    public static string GetRankName(int trophy)
    {
        if (trophy < 200) return "Bronze";
        if (trophy < 400) return "Silver";
        if (trophy < 700) return "Gold";
        if (trophy < 1000) return "Crystal";
        if (trophy < 1400) return "Master";
        return "Legend";
    }

    public static SeasonRewardTier GetRewardTierForTrophy(int trophy)
    {
        lock (_lock)
        {
            var tier = Config.RewardTiers
                .OrderByDescending(t => t.MinTrophy)
                .FirstOrDefault(t => trophy >= t.MinTrophy && trophy <= t.MaxTrophy);

            return tier ?? new SeasonRewardTier();
        }
    }

    public static SeasonSummary OpenNewSeason(string seasonName, DateTime startUtc, DateTime endUtc, bool hardReset, int resetTrophyTo, List<SeasonRewardTier>? rewardTiers)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(seasonName))
                seasonName = $"Season {Config.CurrentSeasonId + 1}";

            Config.CurrentSeasonId = Math.Max(1, Config.CurrentSeasonId + 1);
            Config.SeasonName = seasonName;
            Config.IsActive = true;
            Config.StartTimeUtc = startUtc;
            Config.EndTimeUtc = endUtc;
            Config.HardResetTrophies = hardReset;
            Config.ResetTrophyTo = resetTrophyTo;
            if (rewardTiers != null && rewardTiers.Count > 0)
                Config.RewardTiers = rewardTiers;

            var summary = new SeasonSummary
            {
                SeasonId = Config.CurrentSeasonId,
                SeasonName = Config.SeasonName,
                StartTimeUtc = startUtc,
                EndTimeUtc = endUtc,
                ClosedAtUtc = DateTime.MinValue,
                PlayerCount = AccountCache.Count(),
                RewardedPlayerCount = 0,
                ResetApplied = false
            };

            Save();
            if (hardReset)
                HardResetAllPlayersTo(resetTrophyTo);
            return summary;
        }
    }

    public static SeasonSummary OpenConfiguredSeason(SeasonConfig incoming)
    {
        if (incoming == null) return new SeasonSummary();

        return OpenNewSeason(
            incoming.SeasonName,
            incoming.StartTimeUtc == default ? DateTime.UtcNow : incoming.StartTimeUtc,
            incoming.EndTimeUtc == default ? DateTime.UtcNow.AddDays(60) : incoming.EndTimeUtc,
            incoming.HardResetTrophies,
            incoming.ResetTrophyTo,
            incoming.RewardTiers
        );
    }

    public static SeasonSummary CloseCurrentSeason(bool grantRewards, bool resetPlayers)
    {
        lock (_lock)
        {
            Config.IsActive = false;

            int rewardedPlayers = 0;
            var now = DateTime.UtcNow;

            foreach (var account in AccountCache.GetAllAccounts())
            {
                if (account == null) continue;

                lock (account.SyncLock)
                {
                    if (account.SeasonId == 0)
                        EnsureAccountSeasonState(account);

                    var history = new SeasonHistoryEntry
                    {
                        SeasonId = account.SeasonId,
                        SeasonName = Config.SeasonName,
                        StartTimeUtc = Config.StartTimeUtc,
                        EndTimeUtc = Config.EndTimeUtc,
                        FinalTrophy = account.Trophy,
                        PeakTrophy = account.SeasonPeakTrophy,
                        RankName = GetRankName(account.Trophy),
                        Wins = account.SeasonWins,
                        Losses = account.SeasonLosses,
                        MatchesPlayed = account.SeasonMatchesPlayed,
                        ClaimedAtUtc = now
                    };

                    if (grantRewards)
                    {
                        var tier = GetRewardTierForTrophy(account.Trophy);
                        history.RewardCoins = tier.Coins;
                        history.RewardGems = tier.Gems;
                        history.RewardChests = tier.Chests;
                        history.RewardTitle = tier.TitleReward;
                        rewardedPlayers++;

                        account.Coins += tier.Coins;
                        account.Gems += tier.Gems;
                    }

                    account.SeasonsData.Add(history);

                    if (resetPlayers)
                    {
                        account.SeasonId = 0;
                    }
                    else
                    {
                        account.SeasonRewardClaimed = grantRewards;
                    }
                }
            }

            var summary = new SeasonSummary
            {
                SeasonId = Config.CurrentSeasonId,
                SeasonName = Config.SeasonName,
                StartTimeUtc = Config.StartTimeUtc,
                EndTimeUtc = Config.EndTimeUtc,
                ClosedAtUtc = now,
                PlayerCount = AccountCache.Count(),
                RewardedPlayerCount = rewardedPlayers,
                ResetApplied = resetPlayers
            };

            Config.History.Add(summary);
            Save();
            AccountManager.SaveAccounts();
            return summary;
        }
    }

    public static void HardResetAllPlayersTo(int trophyValue)
    {
        foreach (var account in AccountCache.GetAllAccounts())
        {
            if (account == null) continue;
            lock (account.SyncLock)
            {
                account.Trophy = trophyValue;
                account.SeasonId = Config.CurrentSeasonId;
                account.SeasonStartTrophy = trophyValue;
                account.SeasonPeakTrophy = trophyValue;
                account.SeasonWins = 0;
                account.SeasonLosses = 0;
                account.SeasonMatchesPlayed = 0;
                account.SeasonRewardClaimed = false;
            }
        }
        AccountManager.SaveAccounts();
    }

    private static SeasonConfig CreateDefaultConfig()
    {
        return new SeasonConfig
        {
            CurrentSeasonId = 1,
            SeasonName = "Season 1",
            IsActive = true,
            StartTimeUtc = DateTime.UtcNow,
            EndTimeUtc = DateTime.UtcNow.AddDays(60),
            HardResetTrophies = true,
            ResetTrophyTo = 0,
            RewardTiers = new List<SeasonRewardTier>
            {
                new SeasonRewardTier { MinTrophy = 0, MaxTrophy = 199, Coins = 100, Gems = 0, Chests = 0, TitleReward = "" },
                new SeasonRewardTier { MinTrophy = 200, MaxTrophy = 399, Coins = 200, Gems = 10, Chests = 0, TitleReward = "" },
                new SeasonRewardTier { MinTrophy = 400, MaxTrophy = 699, Coins = 350, Gems = 25, Chests = 1, TitleReward = "" },
                new SeasonRewardTier { MinTrophy = 700, MaxTrophy = 999, Coins = 500, Gems = 50, Chests = 1, TitleReward = "Season Veteran" },
                new SeasonRewardTier { MinTrophy = 1000, MaxTrophy = int.MaxValue, Coins = 1000, Gems = 100, Chests = 3, TitleReward = "Season Legend" }
            }
        };
    }
}
