using System;
public class Cmdhandler
{
    public static void Start()
    {
        Console.WriteLine("[CMD HANDLER] aktif!");
        while (true)
        {
            try
            {
                string cmd = Console.ReadLine();
                if (cmd == null) continue;
                if (!cmd.StartsWith("/")) continue;
                cmd = cmd.Substring(1);
                string[] args = cmd.Split(" ");
                if (args.Length < 1) continue;
                switch (args[0])
                {
                    case "addpre":
                        if (args.Length != 2) Console.WriteLine("kullanım: /addpre (accid)");
                        else
                            Addpremium(args[1]);
                        break;
                    case "removepre":
                        if (args.Length != 2) Console.WriteLine("kullanım: /removepre (accid)");
                        else
                            Removepremium(args[1]);
                        break;
                    case "setcolorid":
                        if (args.Length != 3) Console.WriteLine("kullanım: /setcolorid (accid) (colorid)");
                        else
                            Setcolorid(args[1], args[2]);
                        break;
                    case "help":
                        Console.WriteLine("/help - bu komut\n /addpre (accid) - hesaba premium ekler\n /removepre (accid) - hesaptan premium'u kaldırır\n /setcolorid (accid) (colorid) - hesbaın isim rengine müdahale eder\n /clearcmd - cmd temizler\n /accinfo (id) - account info verilir");
                        break;
                    case "addcoin":
                        break;
                    case "accinfo":
                        if (args.Length != 2) Console.WriteLine("kullanım: /accinfo (accid)");
                        else
                            AccountManager.Getaccountinfo(args[1]);
                        break;
                    case "clearcmd":
                        Console.Clear();
                        break;
                    case "bakım":
                        // bakımal(tahmini süre)???!
                        break;
                    case "bakım-kapat":
                        break;
                    case "testcreateacc":
                        AccountManager.CreateAccount("tr");
                        break;
                    case "saveaccs":
                        AccountManager.SaveAccounts();
                        break;
                    case "sendfriends":
                        if (args.Length != 2) Console.WriteLine("kullanım: /sendfriends (accid)");
                        else
                            Sendfakefriendsrequest(args[1]);
                        break;
                    case "restartserver": // todo....
                        break;
                    case "allistekdelete":
                        if (args.Length != 2) Console.WriteLine("kullanım: /allistekdelete (accid)");
                        else
                            DeleteAllİstek(args[1]);
                        break;
                    case "sendbildirim":
                        // if (args.Length != 3) Console.WriteLine("kullanım: /sendbildirim (accid) (message)");
                        //else
                        SendNotfication(args[1], args[2]);
                        break;
                    case "createclub":
                        if (args.Length != 2) Console.WriteLine("kullanım: /createclub (club name)");
                        else
                            CreateClub(20, args[1]);
                        break;
                    case "ban":
                        if (args.Length < 1 || args.Length < 4) Console.WriteLine("kullanım: /ban (accountid) (sebep(opsiyonel))");
                        else
                            AccountBan(args[1], args[2]);
                            break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("ilgili komut bulunamadı. komutlara erişmek için /help komutunu deneyin");
                        Console.ResetColor();

                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.errorslog("[CMD HANDLER] hata: " + ex.Message);

            }

        }
    }
    public static void Addpremium(string id)
    {


        var account = AccountManager.LoadAccount(id);
        if (account != null)
        {
            account.Premium += 1; // date eklemedik şimdilik
            AccountManager.SaveAccounts();
            Logger.genellog($"{account.Username}({account.AccountId})'ye premium eklendi bitiş tarihi: {account.PremiumEndTime}");


        }
        else
            Logger.errorslog($"[cmd handler] {id} ile aranan oyuncunun hesabı bulunamadı");


    }
    private static void Removepremium(string id)
    {
        var account = AccountManager.LoadAccount(id);
        if (account != null)
        {
            if (account.Premium > 0)
            {
                account.Premium = 0;
                Logger.genellog($"{account.Username} ({account.AccountId})'nin premium'u kaldırıldı.");
            }
            else
                Logger.errorslog($"{account.Username} ({account.AccountId})'nin premium'u zaten yok?! : {account.Premium}");
        }
        else
            Logger.errorslog($"[cmd handler] {id} ile aranan oyuncunun hesabı bulunamadı");
    }
    private static void Setcolorid(string id, string colorids)
    {
        var account = AccountManager.LoadAccount(id);
        if (account != null)
        {
            try
            {
                int colorid = Convert.ToInt32(colorids);
                account.Namecolorid = colorid;
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[CMD HANDLER - SETCOLORİD] hata: {ex.Message} ");
            }
        }
    }

    private static void Sendfakefriendsrequest(string id)
    {
        var acccount = AccountManager.LoadAccount(id);
        string fakeid = "3BT56SDS";
        acccount.bekleyenistekler.Add(new FriendInfo
        {
            Id = fakeid,
            AvatarId = 1,
            Username = "test"
        });
      
        Logger.genellog($"{acccount.Username} ({acccount.AccountId}) kişisini arkadaş ekleme sistemi test etme işlemi başladı...");
    }
    private static void DeleteAllİstek(string id)
    {
        AccountManager.AccountData acccount = AccountManager.LoadAccount(id);
      

        AccountManager.SaveAccounts();
        Console.WriteLine("tüm hesaplar silindi");
    }
    private static void SendNotfication(string id, string message)
    {
        id = "ZSMMXQ1F";
        AccountManager.AccountData acccount = AccountManager.LoadAccount(id);
        if (acccount != null)
        {
           

        }
    }
    private static void CreateClub(int count, string name)
    {
        int index = 0;
        for (int i = 0; i < count; i++)
        {
            string des = name + "aciklama" + index;
            ClubManager.CreateClub(name + index, des, "ZSMMXQ1F");
            index++;
        }
        Logger.genellog(" Toplam  oluşturulan klan: " + index);
    }
    private static void AccountBan(string id, string sebep)
    {
        AccountManager.AccountData account = AccountCache.Load(id);
        if (account != null)
        {

            AccountManager.Ban(account, sebep);
            if (SessionManager.IsOnline(account.AccountId))
            {
                Session session = SessionManager.GetSession(account.AccountId);
                if (session != null)
                {
                    Loginfailed.Send(session, "Hesabınız banlandı", 99);
                    SessionManager.RemoveSession(account.AccountId);
                }
            }                 
        }
    }
}