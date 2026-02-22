    using System.IO;
using Newtonsoft.Json;

   
public enum ContextMode
{
    Friend,
    Club,
    TeamChat,
    CLubChat
}
[PacketHandler(MessageType.ReportPlayerRequest)]
public static class ReportManager
{
 private static List<string> bannedWords;
    public static void Handle(Session session, byte[] data)
    {
        string accountıd;
        ContextMode mode;
        byte messageid;
        using (ByteBuffer read = new ByteBuffer())
        {
            read.WriteBytes(data);
            read.ReadInt();
            mode = (ContextMode)read.ReadByte();
            messageid = read.ReadByte();
        }
        Console.WriteLine("mode: "+ mode );
        if (session.Account == null) return;
        AccountManager.AccountData acc = session.Account;

       
        

        switch (mode)
        {
            case ContextMode.CLubChat:
                SearchingClub(messageid,acc);
                break;
            case ContextMode.TeamChat:
                SearchingChat(session,messageid,acc);
                break;
            default:
                Console.WriteLine("mode bulunamadı");
                break;
        }

    }
    private static void SearchingClub(int messageid,AccountManager.AccountData account)
    {
        if (account.Clubid == -1 || messageid == 0)
        {
            Console.WriteLine("clubid veya messageid sıkıntılı messgeid: " + messageid);
            return;
        } 
       Club club = ClubCache.Load(account.Clubid);
        if (club == null)
        {
            Console.WriteLine("club null");
            return;
        } 
         Console.WriteLine($"========================================");
        Console.WriteLine($"[REPORT CLUB] Kulüp: {club.ClubName ?? "Bilinmiyor"}");
        Console.WriteLine($"[REPORT CLUB] Raporlayan: {account.Username}");
      //  Console.WriteLine($"[REPORT CLUB] Hedef: {targetaccount.Username}");
        Console.WriteLine($"[REPORT CLUB] Mesaj ID: {messageid}");
        Console.WriteLine($"========================================");
      //  if (messageid - 10 < 0) return; // demekki 10dan daha az mesaj var
        Console.WriteLine($"-----Önceki mesajlar----");
        for (int backmsg = messageid; backmsg > messageid - 10 && backmsg >= 0; backmsg--)
        {
            ClubMessage message = ClubManager.GetCLubMessage(club, backmsg);
            if (message == null)
            {
                Console.WriteLine("böyle bi mesaj bulunamadı");
                continue;
            }
            if (message.messageFlags != ClubMessageFlags.None) continue;
            Console.WriteLine($"[{backmsg}] gönderen: {message.SenderName}, conent: {message.Content} tarih: {message.Timestamp}");
        }
        Console.WriteLine($"---------");
         Console.WriteLine($"-----Sonraki mesajlar----");
        for (int backmsg = messageid+1; backmsg < messageid + 10; backmsg++)
        {
            ClubMessage message = ClubManager.GetCLubMessage(club, backmsg);
            
            if (message == null)
            {
                Console.WriteLine("böyle bi mesaj bulunamadı");
                continue;
            }
             if (message.messageFlags != ClubMessageFlags.None) continue;
            Console.WriteLine($"[{backmsg}] gönderen: {message.SenderName}, conent: {message.Content} tarih: {message.Timestamp}");
        }
        Console.WriteLine($"---------");
         
    }
    private static void SearchingChat(Session session, int messageid, AccountManager.AccountData account)
    {
        if (session.TeamID == -1 && messageid == 0) return;
        Lobby lobby = LobbyManager.GetLobby(session.TeamID);
        if (lobby == null) return;
        //  if (messageid - 10 < 0) return; // demekki 10dan daha az mesaj var
        Console.WriteLine($"-----Önceki mesajlar----");
        for (int backmsg = messageid; backmsg > messageid - 10 && backmsg >= 0; backmsg--)
        {
            TeamMessage message = LobbyManager.GetMessage(lobby, backmsg);
            if (message == null)
            {
                Console.WriteLine("böyle bi mesaj bulunamadı");
                continue;
            }
            if (message.messageFlags != TeamMessageFlags.None) continue;
            Console.WriteLine($"gönderen: {message.SenderName} conent: {message.Content} tarih: {message.Timestamp}");
        }
        Console.WriteLine($"---------");
        Console.WriteLine($"-----Sonraki mesajlar----");
        for (int backmsg = messageid; backmsg < messageid + 10; backmsg++)
        {
            TeamMessage message = LobbyManager.GetMessage(lobby, backmsg);

            if (message == null)
            {
                Console.WriteLine("böyle bi mesaj bulunamadı");
                continue;
            }
            if (message.messageFlags != TeamMessageFlags.None) continue;
            Console.WriteLine($"gönderen: {message.SenderName} conent: {message.Content} tarih: {message.Timestamp}");
        }
        Console.WriteLine($"---------");
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
            Console.WriteLine("[BannedWordManager] Banned word JSON file not found.");
            bannedWords = new List<string>();
        }
    }

    public static bool IsBannedWord(string word)
    {
    
            if (bannedWords == null)
            return false;

        return bannedWords.Contains(word.ToLower());
    }


}


