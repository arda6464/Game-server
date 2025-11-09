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
                        Console.WriteLine("/help - bu komut\n /addpre (accid) - hesaba premium ekler\n /removepre (accid) - hesaptan premium'u kaldırır\n /setcolorid (accid) (colorid) - hesbaın isim rengine müdahale eder\n /clearcmd - cmd temizler\n /accinfo (id) - account info verilir \n /saveacss - tüm hesapları kaydeder \n /sendfriends - belirli bir hesaba belirli bir hesaptan istek atar \n /allistekdelete  - hesabın  tüm  friends isteklerini temizler \n /sendbildirim- banner ile bildirim yollar \n /createclub - 20 kulüp oluşturur \n /ban - hesap banlar \n /showprofile - belirli hesabın profiline baktırır \n /DeleteAllNotfications - hesapların tüm bildirim geçmişini siler \n ");
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
                    case "restartserver":
                        Console.WriteLine("Sunucu yeniden başlatılıyor...");
                        Logger.genellog("[CMD] Sunucu restart komutu alındı");
                        // Tüm bağlantıları kapat ve temizle
                        foreach (var session in SessionManager.GetSessions().ToList())
                        {
                            session.Value.Close();
                        }
                        AccountCache.Stop();
                        ClubManager.Save();
                        AccountManager.SaveAccounts();
                        Environment.Exit(0); // Programı sonlandır
                        break;
                    case "allistekdelete":
                        if (args.Length != 2) Console.WriteLine("kullanım: /allistekdelete (accid)");
                        else
                            DeleteAllİstek(args[1]);
                        break;
                    case "sendbildirim":
                        Console.WriteLine("gelen lenght:" + args.Length);
                        if (args.Length < 2 || args.Length > 6) Console.WriteLine("kullanım: /sendbildirim (notficaiton id) (message) posiyonel: (acıklama)(url)");
                        else
                        {
                            int id = Convert.ToInt32(args[1]);
                            int index = args.Length;
                            switch (index)
                            {
                                case 3:
                                  //  SendNotfication(id, args[2]);
                                    break;
                                case 4:
                                   // SendNotfication(id, args[2], args[3]);
                                    break;
                                case 5:
                                   
                                   // SendNotfication(id, args[2], args[3], args[4]);
                                    break;

                            }


                        }
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
                    case "unban":
                        if (args.Length < 1 || args.Length < 4) Console.WriteLine("kullanım: /ban (accountid) (sebep(opsiyonel))");
                        else
                           Unban(args[1]);
                        break;
                    case "showprofile":
                        if (args.Length != 2) Console.WriteLine("kullanım: /showprofile (accountıd)");
                        else
                            ProfileShow(args[1]);
                        break;
                    case "deleteAllNotfications":
                        AccountManager.DeleteNotfications();
                        break;
                    case "inbox":
                        Sendinboxmessage();
                        break;
                    case "ResetAccount":
                        ResetAccount();
                        break;
                    case "Porno":
                        PornoTest();
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
        var acccount = AccountCache.Load("WHM7ZVYY");
        string fakeid = "7LRLRJZ6";
        acccount.Requests.Add(new FriendInfo
        {
            Id = fakeid,
            AvatarId = 3,
            Username = "test31"
        });

        Logger.genellog($"{acccount.Username} ({acccount.AccountId}) kişisini arkadaş ekleme sistemi test etme işlemi başladı...");
    }
    private static void DeleteAllİstek(string id)
    {
        AccountManager.AccountData acccount = AccountManager.LoadAccount(id);


        AccountManager.SaveAccounts();
        Console.WriteLine("tüm hesaplar silindi");
    }
   /* private static void SendNotfication(int id, string message, string acıklamna = "", string url = "time brawl")
    {
        string accid = "WHM7ZVYY";
        AccountManager.AccountData acccount = AccountCache.Load(accid);
        if (acccount != null)
        {
           Notification notification = new Notification(
    id,
    message,
    acıklamna,
    url
);
            NotificationManager.Add(acccount, notification);
            if (SessionManager.IsOnline(acccount.AccountId))
            {
                Session session = SessionManager.GetSession(acccount.AccountId);
                NotificationSender.Send(session, notification);
            }
            else
                Console.WriteLine("oyuncu aktif değil");
            Logger.genellog($"{acccount.Username} adlı kullanıcısına {notification} bildirimi eklendi");
        }
    }*/
    private static void CreateClub(int count, string name)
    {
        int index = 0;
        for (int i = 0; i < count; i++)
        {
            string des = name + "aciklama" + index;
            ClubManager.CreateClub(name + index, des, 1,"WHM7ZVYY");
            index++;
        }
        Logger.genellog(" Toplam  oluşturulan klan: " + index);
    }
    private static void AccountBan(string id, string sebep)
    {
        AccountManager.AccountData account = AccountCache.Load(id);
        if (account != null)
        {
            BanManager.BanPlayer(account.AccountId,"Sistem","PORNOMATİK HİLELER KULLANMASI", false, TimeSpan.FromMinutes(5));
           
            if (SessionManager.IsOnline(account.AccountId))
            {
                Session session = SessionManager.GetSession(account.AccountId);
                if (session != null)
                {
                    string banmesage = BanManager.GetBanMessage(account.AccountId);
                    Loginfailed.Send(session, banmesage, 99);
                    SessionManager.RemoveSession(account.AccountId);
                }
            }
        }
    }
    private static void ProfileShow(string acccountId)
    {
        Session session = SessionManager.GetSession(acccountId);
        ShowProfileHandler.test(session);
        Console.WriteLine("profile test runing");
    }
    private static void Sendinboxmessage()
    {
        Console.WriteLine("slm");
        İnboxNotfication inbox = new İnboxNotfication
        {
            ID = 12,
            Sender = "Sistem",
            Message = "Teşekkür ederiz",
            Timespam = DateTime.Now
        };
        string accid = "0FU8YO95";
        AccountManager.AccountData acccount = AccountManager.LoadAccount(accid);
          acccount.inboxesNotfications.Add(inbox);
        if (SessionManager.IsOnline(acccount.AccountId))
        {
            Session session = SessionManager.GetSession(acccount.AccountId);
            NotificationSender.İnboxSend(session, inbox);
          
        }
        else
        {
            Console.WriteLine("oyuncu aktif değil");
            Logger.genellog($"{acccount.Username} adlı kullanıcısına  bildirim eklendi");
        }

    }
    private static void ResetAccount()
    {
        Console.WriteLine("Hesap resetleniyor...");
        AccountManager.AccountData account = AccountCache.Load("XW2RY9UI");
        if (account == null)
        {
            Logger.errorslog("[ResetAccount] Account bulunamadı!");
            return;
        }
        if(account.Clubid != -1)
        {
            ClubManager.RemoveMember(account.Clubid, account.AccountId);
        }
        // Hesabı sıfırla
        account.Clubid = -1;
        account.clubRole = ClubRole.Member;
        account.Level = 1;
        account.Gems = 0;
        account.Avatarid = 1;
        account.Namecolorid = 1;
        account.Friends.Clear();
        account.Requests.Clear();
        
        AccountManager.SaveAccounts();
        Logger.genellog($"[ResetAccount] {account.Username} hesabı resetlendi");
    }

    private static void PornoTest()
    {
        string id = "0FU8YO95";
        var acccount = AccountCache.Load(id);
        AccountManager.AddRole(acccount, Role.Roles.Owner);

    }
    private static void Unban(string accountid)
    {
        BanManager.UnbanPlayer(accountid,"Sistem", "Yanlış yasaklama");
    }
   
}