    using System.IO;
using Newtonsoft.Json;

   
public enum ContextMode
{
    Friend,
    Club,
    TeamChat,
    CLubChat
}

public class ReportMessage
{
    public int SenderId { get; set; }
    public string? SenderName { get; set; }
    public string? Content { get; set; }
    public string? Time { get; set; }
}

public class ReportData
{
    public string? Id { get; set; }
    public int? ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public int? TargetId { get; set; }
    public string? TargetName { get; set; }
    public string? Reason { get; set; }
    public string? Type { get; set; } // Club / Team
    public string? ClubName { get; set; }
    public List<ReportMessage> Context { get; set; } = new List<ReportMessage>();
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Resolved
}

[PacketHandler(MessageType.ReportPlayerRequest)]
public static class ReportManager
{
    private static List<ReportData> reports = new List<ReportData>();
    private static List<string>? bannedWords;
    private static readonly string ReportsFile = "reports.json";

    public static void Init()
    {
        LoadReports();
        LoadBannedWords();
    }

    private static void LoadReports()
    {
        if (File.Exists(ReportsFile))
        {
            try
            {
                reports = JsonConvert.DeserializeObject<List<ReportData>>(File.ReadAllText(ReportsFile)) ?? new List<ReportData>();
            }
            catch { reports = new List<ReportData>(); }
        }
    }

    private static void SaveReports()
    {
        try
        {
            File.WriteAllText(ReportsFile, JsonConvert.SerializeObject(reports, Formatting.Indented));
        }
        catch (Exception ex) { Console.WriteLine("[ReportManager] Save error: " + ex.Message); }
    }

    public static List<ReportData> GetReports() => reports;

    public static bool ResolveReport(string reportId)
    {
        var report = reports.Find(r => r.Id == reportId);
        if (report != null)
        {
            report.Status = "Resolved";
            SaveReports();
            return true;
        }
        return false;
    }

    public static bool DeleteReport(string reportId)
    {
        var report = reports.Find(r => r.Id == reportId);
        if (report != null)
        {
            reports.Remove(report);
            SaveReports();
            return true;
        }
        return false;
    }

    public static void Handle(Session session, byte[] data)
    {
        ContextMode mode;
        byte messageid;
        using (ByteBuffer read = new ByteBuffer())
        {
            read.WriteBytes(data);
            read.ReadVarInt(); // Length
            mode = (ContextMode)read.ReadVarInt();
            messageid = (byte)read.ReadVarInt();
        }

        if (session.Account == null) return;
        AccountManager.AccountData acc = session.Account;

        switch (mode)
        {
            case ContextMode.CLubChat:
                CreateClubReport(messageid, acc);
                break;
            case ContextMode.TeamChat:
                CreateTeamReport(session, messageid, acc);
                break;
            default:
                Console.WriteLine("[ReportManager] Geçersiz mod: " + mode);
                break;
        }
    }

    private static void CreateClubReport(int messageid, AccountManager.AccountData account)
    {
        if (account.Clubid == -1 || messageid == 0) return;
        Club club = ClubCache.Load(account.Clubid);
        if (club == null) return;

        var report = new ReportData
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            ReporterId = account.ID,
            ReporterName = account.Username,
            Type = "Club",
            ClubName = club.ClubName,
            Timestamp = DateTime.Now,
            Status = "Pending",
            Reason = "Sohbet İhlali"
        };

        // Bağlam mesajlarını topla (-5, +5)
        for (int i = messageid - 5; i <= messageid + 5; i++)
        {
            if (i < 0) continue;
            ClubMessage msg = ClubManager.GetCLubMessage(club, i);
            if (msg != null && msg.messageFlags == ClubMessageFlags.None)
            {
                if (i == messageid)
                {
                    report.TargetName = msg.SenderName;
                    report.TargetId = msg.SenderId;
                }
                report.Context.Add(new ReportMessage 
                { 
                    SenderId = msg.SenderId,
                    SenderName = msg.SenderName, 
                    Content = msg.Content, 
                    Time = msg.Timestamp.ToString("HH:mm:ss") 
                });
            }
        }

        reports.Add(report);
        SaveReports();
        Console.WriteLine($"[ReportManager] Yeni kulüp raporu oluşturuldu: {report.Id} (Reporter: {report.ReporterName})");
    }

    private static void CreateTeamReport(Session session, int messageid, AccountManager.AccountData account)
    {
        if (session.TeamID == -1 || messageid == 0) return;
        Lobby lobby = LobbyManager.GetLobby(session.TeamID);
        if (lobby == null) return;

        var report = new ReportData
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            ReporterId = account.ID,
            ReporterName = account.Username,
            Type = "Team",
            Timestamp = DateTime.Now,
            Status = "Pending",
            Reason = "Lobi Sohbet İhlali"
        };

        for (int i = messageid - 5; i <= messageid + 5; i++)
        {
            if (i < 0) continue;
            TeamMessage msg = LobbyManager.GetMessage(lobby, i);
            if (msg != null && msg.messageFlags == TeamMessageFlags.None)
            {
                if (i == messageid)
                {
                    report.TargetName = msg.SenderName;
                    report.TargetId = msg.SenderId;
                }
                report.Context.Add(new ReportMessage 
                { 
                    SenderId = msg.SenderId,
                    SenderName = msg.SenderName, 
                    Content = msg.Content, 
                    Time = msg.Timestamp.ToString("HH:mm:ss") 
                });
            }
        }

        reports.Add(report);
        SaveReports();
        Console.WriteLine($"[ReportManager] Yeni takım raporu oluşturuldu: {report.Id} (Reporter: {report.ReporterName})");
    }

    public static void LoadBannedWords()
    {
        if (File.Exists("bannedword.json"))
        {
            try
            {
                string json = File.ReadAllText("bannedword.json");
                bannedWords = JsonConvert.DeserializeObject<List<string>>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BannedWordManager] Banned word JSON parse error: {ex.Message}");
            }
        }
        else
        {
            bannedWords = new List<string>();
        }
    }

    public static bool IsBannedWord(string word)
    {
        if (bannedWords == null) return false;
        return bannedWords.Contains(word.ToLower());
    }
}
