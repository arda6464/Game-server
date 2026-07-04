using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using GachaSystem;

public class CmdHandler
{
    #region Fields & Properties
    private static readonly Dictionary<string, CommandAction> _commands = new();
    private static readonly Dictionary<string, string> _aliases = new();
    private static bool _isRunning = true;
    private static readonly object _lock = new();
    private static readonly List<string> _commandHistory = new();
    private static int _historyIndex = -1;
    #endregion

    #region Command Delegate
    private delegate void CommandAction(string[] args);
    #endregion

    #region Initialization
    public static void Start()
    {


        RegisterCommands();
        //RegisterAliases();

        while (_isRunning)
        {
            try
            {
                Console.Write("> ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                // Komut geçmişi
                _commandHistory.Add(input);
                _historyIndex = _commandHistory.Count;

                // Escaped characters işleme
                input = ProcessEscapedInput(input);

                if (!input.StartsWith("/"))
                {
                    // Normal metin modu (opsiyonel)
                    continue;
                }

                ProcessCommand(input.Substring(1));
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[CMD] Hata: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
    #endregion

    #region Command Registration
    private static void RegisterCommands()
    {
        // Kullanıcı yönetimi
        Register("addpre", "Hesaba premium ekler", AddPremium, "ap", "premiumadd");
        Register("removepre", "Premium kaldırır", RemovePremium, "rp", "premiumremove");
        Register("setcolor", "İsim rengi değiştirir", SetColor, "sc", "color");
        Register("accinfo", "Hesap bilgisi gösterir", AccountInfo, "ai", "info");
        Register("showprofile", "Profil gösterir", ShowProfile, "sp", "profile");
        Register("sendgacha", "test ödülleri gönderir", GiveReward, "gacha");

        // Yetki yönetimi
        Register("mute", "Oyuncuyu susturur", Mute, "m");
        Register("unmute", "Susturmayı kaldırır", Unmute, "um");
        Register("ban", "Oyuncuyu yasaklar", Ban, "b");
        Register("unban", "Yasağı kaldırır", Unban, "ub");
        Register("deleteaccounts", "tüm hesapları siler", DeleteAllAccount, "deleteaccs", "daccs");
        Register("deleteclubs", "tüm kulüpleri siler", DeleteAllClubs, "deleteclubs", "dclubs");

        // Sistem yönetimi
        Register("save", "Tüm verileri kaydeder", SaveAll, "s", "saveall");
        Register("restart", "Sunucuyu yeniden başlatır", RestartServer, "rs", "reboot");
        Register("clear", "Ekranı temizler", ClearConsole, "cls");
        Register("exit", "Sunucuyu kapatır", Exit, "quit", "q");

        // Oyun ayarları
        Register("matchmaking", "Matchmaking ayarları", MatchmakingConfig, "mm", "mmconfig");
        Register("setmaxplayers", "Max oyuncu sayısını ayarlar", SetMaxPlayers, "smp");

        // Bilgi
        Register("help", "Komut listesini gösterir", ShowHelp, "h", "?");
        Register("stats", "Sunucu istatistiklerini gösterir", ShowStats, "st");
        Register("online", "Online oyuncuları listeler", ShowOnline, "ol");
        Register("history", "Komut geçmişini gösterir", ShowHistory, "hist");
    }

    private static void Register(string command, string description, CommandAction action, params string[] aliases)
    {
        _commands[command.ToLower()] = action;

        foreach (var alias in aliases)
        {
            _aliases[alias.ToLower()] = command.ToLower();
        }

        // CommandInfo'yu ayrı bir yerde tutabiliriz
        CommandRegistry.Register(command, description, aliases);
    }
    #endregion

    #region Command Processing
    private static void ProcessCommand(string input)
    {
        var args = ParseArguments(input);
        if (args.Length == 0) return;

        string cmd = args[0].ToLower();

        // Alias kontrolü
        if (_aliases.TryGetValue(cmd, out string? realCommand))
        {
            cmd = realCommand;
        }

        if (_commands.TryGetValue(cmd, out CommandAction? action))
        {
            try
            {
                action(args);
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[CMD] '{cmd}' çalıştırılırken hata: {ex.Message}");
                WriteError($"Komut çalıştırılamadı: {ex.Message}");
            }
        }
        else
        {
            WriteWarning($"Bilinmeyen komut: {cmd}. /help yazın.");
        }
    }

    private static string[] ParseArguments(string input)
    {
        // Tırnak içindeki argümanları destekle
        var matches = Regex.Matches(input, @"(?<match>\w+)|""(?<match>[\w\s]*)""");
        return matches.Select(m => m.Groups["match"].Value).ToArray();
    }

    private static string ProcessEscapedInput(string input)
    {
        // Kaçış karakterlerini işle
        return input.Replace("\\n", "\n").Replace("\\t", "\t");
    }
    #endregion

    #region Command Implementations
    private static void AddPremium(string[] args)
    {
        if (args.Length != 2)
        {
            WriteUsage("/addpre <ID>");
            return;
        }

        var account = ResolveAccount(args[1]);
        if (account == null) return;

        var session = SessionManager.GetSession(account.ID);
        var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
        logic.SetPremium(account.Premium + 1);

        WriteSuccess($"{account.Username} premium seviyesi artırıldı. (Yeni seviye: {account.Premium + 1})");
    }

    private static void RemovePremium(string[] args)
    {
        if (args.Length != 2)
        {
            WriteUsage("/removepre <ID>");
            return;
        }

        var account = ResolveAccount(args[1]);
        if (account == null) return;

        var session = SessionManager.GetSession(account.ID);
        var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
        logic.RemovePremium();

        WriteSuccess($"{account.Username} premium üyeliği kaldırıldı.");
    }

    private static void SetColor(string[] args)
    {
        if (args.Length != 3)
        {
            WriteUsage("/setcolor <ID> <colorId>");
            return;
        }

        if (!int.TryParse(args[2], out int colorId))
        {
            WriteError("Geçersiz color ID. Sayısal bir değer girin.");
            return;
        }

        var account = ResolveAccount(args[1]);
        if (account == null) return;

        var session = SessionManager.GetSession(account.ID);
        var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
        logic.SetNameColor(colorId);

        WriteSuccess($"{account.Username} isim rengi {colorId} olarak güncellendi.");
    }

    private static void Mute(string[] args)
    {
        if (args.Length != 3)
        {
            WriteUsage("/mute <ID> <dakika>");
            return;
        }

        if (!int.TryParse(args[2], out int minutes))
        {
            WriteError("Geçersiz dakika değeri.");
            return;
        }

        var account = ResolveAccount(args[1]);
        if (account == null) return;

        var session = SessionManager.GetSession(account.ID);
        var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
        logic.Mute(TimeSpan.FromMinutes(minutes));

        WriteSuccess($"{account.Username} {minutes} dakika susturuldu.");
    }

    private static void Unmute(string[] args)
    {
        if (args.Length != 2)
        {
            WriteUsage("/unmute <ID>");
            return;
        }

        var account = ResolveAccount(args[1]);
        if (account == null) return;

        var session = SessionManager.GetSession(account.ID);
        var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
        logic.Unmute();

        WriteSuccess($"{account.Username} susturması kaldırıldı.");
    }

    private static void Ban(string[] args)
    {
        if (args.Length < 2)
        {
            WriteUsage("/ban <ID> [sebep]");
            return;
        }

        var account = ResolveAccount(args[1]);
        if (account == null) return;

        string reason = args.Length > 2 ? string.Join(" ", args.Skip(2)) : "Konsol üzerinden yasaklandı";

        BanManager.BanPlayer(account.ID, "Sistem", reason, true);
        WriteSuccess($"{account.Username} banlandı. Sebep: {reason}");
    }

    private static void Unban(string[] args)
    {
        if (args.Length != 2)
        {
            WriteUsage("/unban <ID>");
            return;
        }

        var account = ResolveAccount(args[1]);
        if (account == null) return;

        BanManager.UnbanPlayer(account.ID, "Sistem", "Konsol üzerinden kaldırıldı");
        WriteSuccess($"{account.Username} banı kaldırıldı.");
    }

    private static void AccountInfo(string[] args)
    {
        if (args.Length != 2)
        {
            WriteUsage("/accinfo <ID>");
            return;
        }

        var account = ResolveAccount(args[1]);
        if (account == null) return;

        AccountManager.Getaccountinfo(account.ID);
    }

    private static void ShowProfile(string[] args)
    {
        if (args.Length != 2)
        {
            WriteUsage("/showprofile <ID>");
            return;
        }

        var account = ResolveAccount(args[1]);
        if (account == null) return;

        if (!SessionManager.IsOnline(account.ID))
        {
            WriteWarning($"{account.Username} çevrimdışı.");
            return;
        }

        var session = SessionManager.GetSession(account.ID);
        //  ShowProfileHandler.Test(session);
        WriteSuccess($"{account.Username} için profil testi çalıştırıldı.");
    }

    private static void SaveAll(string[] args)
    {
        WriteInfo("Veriler kaydediliyor...");
        AccountCache.SaveAll();
        ClubManager.Save();
        AccountManager.SaveAccounts();
        WriteSuccess("Tüm veriler kaydedildi.");
    }

    private static void RestartServer(string[] args)
    {
        WriteWarning("Sunucu yeniden başlatılıyor...");

        Program.SaveDataAndExit();
    }

    private static void ClearConsole(string[] args)
    {
        Console.Clear();
        Console.WriteLine("╔═══════════════════════════════════════╗");
        Console.WriteLine("║      GAME SERVER CMD HANDLER         ║");
        Console.WriteLine("╚═══════════════════════════════════════╝");
        Console.WriteLine();
    }

    private static void Exit(string[] args)
    {
        WriteWarning("Sunucu kapatılıyor...");
        SaveAll(args);
        _isRunning = false;
        Environment.Exit(0);
    }

    private static void MatchmakingConfig(string[] args)
    {
        if (args.Length == 1)
        {
            WriteInfo($"Mevcut maç başı oyuncu sayısı: {MatchMaking.PlayersPerMatch}");
            WriteInfo("Yeni değer girmek için: /matchmaking <sayı>");
            return;
        }

        if (!int.TryParse(args[1], out int value))
        {
            WriteError("Geçersiz sayı.");
            return;
        }

        MatchMaking.PlayersPerMatch = value;
        Logger.genellog($"Maç başı kişi sayısı {value} olarak ayarlandı.");
        WriteSuccess($"Maç başı kişi sayısı {value} olarak ayarlandı.");
    }

    private static void SetMaxPlayers(string[] args)
    {
        // Implementasyon
    }

    private static void ShowStats(string[] args)
    {
        int online = SessionManager.GetSessions().Count;
        int total = AccountCache.Count();
        int banned = BanManager.GetActiveBans().Count;

        WriteInfo("═══════════════ SUNUCU İSTATİSTİKLERİ ═══════════════");
        WriteInfo($"👥 Online Oyuncu: {online}");
        WriteInfo($"📊 Toplam Hesap: {total}");
        WriteInfo($"🔨 Banlı Oyuncu: {banned}");
        WriteInfo($"⚙️ Maç Başı Oyuncu: {MatchMaking.PlayersPerMatch}");
        WriteInfo($"⏰ Uptime: {GetUptime()}");
        WriteInfo($"💾 Bellek Kullanımı: {GetMemoryUsage()}");
        WriteInfo("═══════════════════════════════════════════════════");
    }

    private static void ShowOnline(string[] args)
    {
        var sessions = SessionManager.GetSessions();
        if (sessions.Count == 0)
        {
            WriteInfo("Çevrimiçi oyuncu yok.");
            return;
        }

        WriteInfo($"══════ ÇEVRİMİÇİ OYUNCULAR ({sessions.Count}) ══════");
        foreach (var session in sessions.Values)
        {
            WriteInfo($"• {session.Account.Username} (ID: {session.Account.ID})");
        }
        WriteInfo("═══════════════════════════════════════════════════");
    }

    private static void ShowHistory(string[] args)
    {
        if (_commandHistory.Count == 0)
        {
            WriteInfo("Komut geçmişi boş.");
            return;
        }

        WriteInfo("══════ KOMUT GEÇMİŞİ ══════");
        int start = Math.Max(0, _commandHistory.Count - 20);
        for (int i = start; i < _commandHistory.Count; i++)
        {
            WriteInfo($"[{i + 1}] {_commandHistory[i]}");
        }
        WriteInfo("════════════════════════════");
    }

    private static void DeleteAllAccount(string[] args)
    {
        DatabaseManager.DeleteAllAccounts(reason: args[1]);
    }
    private static void DeleteAllClubs(string[] args)
    {
        DatabaseManager.DeleteAllClubs(reason: args[1]);
    }
    private static void GiveReward(string[] args)
    {

        int id = Convert.ToInt32(args[1]);
        if (!SessionManager.IsOnline(id)) return;


        Session session = SessionManager.GetSession(id);



        var gacha = new GachaResponsePacket();
        RewardItem reward1 = new RewardItem
        {
            Type = ItemType.Gems,
            Count = 100,
            DataId = 0
        };
        RewardItem reward2 = new RewardItem
        {
            Type = ItemType.Gems,
            Count = 200,
            DataId = 0
        };
        RewardItem reward3 = new RewardItem
        {
            Type = ItemType.Gems,
            Count = 300,
            DataId = 0
        };
        gacha.Drops.Add(new GachaSystem.GachaReward(reward1));
        gacha.Drops.Add(new GachaSystem.GachaReward(reward2));
        gacha.Drops.Add(new GachaSystem.GachaReward(reward3));
        session.Send(gacha);
        Console.WriteLine("Test gachsı gönderildi");

    }

    private static void ShowHelp(string[] args)
    {
        WriteInfo("═══════════════════ KOMUT LİSTESİ ═══════════════════");
        WriteInfo("");

        var orderedCommands = CommandRegistry.GetAll()
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Command);

        string currentCategory = "";

        foreach (var cmd in orderedCommands)
        {
            if (cmd.Category != currentCategory)
            {
                currentCategory = cmd.Category;
                WriteInfo($"── {currentCategory} ──");
            }

            string aliases = cmd.Aliases.Length > 0
                ? $" (Alias: {string.Join(", ", cmd.Aliases)})"
                : "";

            WriteInfo($"  /{cmd.Command,-15} - {cmd.Description}{aliases}");
        }

        WriteInfo("");
        WriteInfo("═══════════════════════════════════════════════════");
        WriteInfo("💡 İpucu: Komutlarda ID yerine kullanıcı adı da kullanabilirsiniz.");
    }
    #endregion

    #region Helper Methods
    private static AccountManager.AccountData? ResolveAccount(string input)
    {
        int id = 0;
        try { id = Convert.ToInt32(input); }
        catch
        {

        }


        var account = AccountCache.Load(id);
        if (account != null)
            return account;

        WriteError($"Hesap bulunamadı: {id}");
        return null;
    }

    private static string GetUptime()
    {
        var uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime;
        return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }

    private static string GetMemoryUsage()
    {
        long memory = GC.GetTotalMemory(false);
        return FormatBytes(memory);
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }

    private static void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }

    private static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {message}");
        Console.ResetColor();
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    private static void WriteUsage(string usage)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Kullanım: {usage}");
        Console.ResetColor();
    }
    #endregion

    #region Command Registry (For Better Organization)
    private static class CommandRegistry
    {
        private static readonly List<CommandInfo> _commands = new();

        public static void Register(string command, string description, string[] aliases, string category = "Genel")
        {
            _commands.Add(new CommandInfo
            {
                Command = command,
                Description = description,
                Aliases = aliases,
                Category = category
            });
        }

        public static IEnumerable<CommandInfo> GetAll() => _commands;

        public class CommandInfo
        {
            public string Command { get; set; }
            public string Description { get; set; }
            public string[] Aliases { get; set; } = Array.Empty<string>();
            public string Category { get; set; } = "Genel";
        }
    }
    #endregion
}