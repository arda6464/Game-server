using System;
using System.Linq;
using System.Collections.Generic;

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
                        if (args.Length != 2) Console.WriteLine("kullanım: /addpre (ID)");
                        else Addpremium(args[1]);
                        break;
                    case "removepre":
                        if (args.Length != 2) Console.WriteLine("kullanım: /removepre (ID)");
                        else Removepremium(args[1]);
                        break;
                    case "setcolorid":
                        if (args.Length != 3) Console.WriteLine("kullanım: /setcolorid (ID) (colorid)");
                        else Setcolorid(args[1], args[2]);
                        break;
                    case "help":
                        Console.WriteLine("/help - bu komut\n /addpre (ID) - hesaba premium ekler\n /removepre (ID) - hesaptan premium'u kaldırır\n /setcolorid (ID) (colorid) - hesabın isim rengine müdahale eder\n /clearcmd - cmd temizler\n /accinfo (ID) - account info verilir \n /saveaccs - tüm hesapları kaydeder \n /mute (ID) (dakika) - oyuncuyu susturur\n /unmute (ID) - oyuncunun susturmasını kaldırır\n /ban (ID) (sebep) - oyuncuyu yasaklar \n /unban (ID) - yasagı kaldırır");
                        break;
                    case "mute":
                        if (args.Length != 3) Console.WriteLine("kullanım: /mute (ID) (dakika)");
                        else
                        {
                            var muteAcc = ResolveAccount(args[1]);
                            if (muteAcc != null)
                            {
                                var sess = SessionManager.GetSession(muteAcc.ID);
                                var logic = sess?.Logic ?? new Logic.AccountLogic(muteAcc, sess);
                                logic.Mute(TimeSpan.FromMinutes(int.Parse(args[2])));
                                Console.WriteLine($"{muteAcc.Username} {args[2]} dakika susturuldu.");
                            }
                        }
                        break;
                    case "unmute":
                        if (args.Length != 2) Console.WriteLine("kullanım: /unmute (ID)");
                        else
                        {
                            var unmuteAcc = ResolveAccount(args[1]);
                            if (unmuteAcc != null)
                            {
                                var sess = SessionManager.GetSession(unmuteAcc.ID);
                                var logic = sess?.Logic ?? new Logic.AccountLogic(unmuteAcc, sess);
                                logic.Unmute();
                                Console.WriteLine($"{unmuteAcc.Username} susturması kaldırıldı.");
                            }
                        }
                        break;
                    case "accinfo":
                        if (args.Length != 2) Console.WriteLine("kullanım: /accinfo (ID)");
                        else
                        {
                            var acc = ResolveAccount(args[1]);
                            if (acc != null) AccountManager.Getaccountinfo(acc.ID);
                        }
                        break;
                    case "clearcmd":
                        Console.Clear();
                        break;
                    case "saveaccs":
                        AccountCache.SaveAll();
                        break;
                    case "ban":
                        if (args.Length < 2) Console.WriteLine("kullanım: /ban (ID) (sebep)");
                        else AccountBan(args[1], args.Length > 2 ? args[2] : "");
                        break;
                    case "unban":
                        if (args.Length != 2) Console.WriteLine("kullanım: /unban (ID)");
                        else Unban(args[1]);
                        break;
                    case "showprofile":
                        if (args.Length != 2) Console.WriteLine("kullanım: /showprofile (ID)");
                        else ProfileShow(args[1]);
                        break;
                    case "restartserver":
                        Console.WriteLine("Sunucu yeniden başlatılıyor...");
                        foreach (var session in SessionManager.GetSessions().ToList()) session.Value.Close();
                        AccountCache.Stop();
                        ClubManager.Save();
                        AccountManager.SaveAccounts();
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Komut bulunamadı. /help yazın.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.errorslog("[CMD HANDLER] hata: " + ex.Message);
            }
        }
    }

    private static AccountManager.AccountData? ResolveAccount(string input)
    {
        if (int.TryParse(input, out int id))
        {
            return AccountCache.Load(id);
        }
        Console.WriteLine("Hata: Geçersiz sayısal ID.");
        return null;
    }

    public static void Addpremium(string input)
    {
        var account = ResolveAccount(input);
        if (account != null)
        {
            var session = SessionManager.GetSession(account.ID);
            var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
            logic.SetPremium(account.Premium + 1);
            Console.WriteLine($"{account.Username} premium seviyesi artırıldı.");
        }
    }

    private static void Removepremium(string input)
    {
        var account = ResolveAccount(input);
        if (account != null)
        {
            var session = SessionManager.GetSession(account.ID);
            var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
            logic.RemovePremium();
            Console.WriteLine($"{account.Username} premium üyeliği kaldırıldı.");
        }
    }

    private static void Setcolorid(string input, string colorids)
    {
        var account = ResolveAccount(input);
        if (account != null && int.TryParse(colorids, out int colorid))
        {
            var session = SessionManager.GetSession(account.ID);
            var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
            logic.SetNameColor(colorid);
            Console.WriteLine($"{account.Username} isim rengi güncellendi.");
        }
    }

    private static void AccountBan(string input, string sebep)
    {
        var account = ResolveAccount(input);
        if (account != null)
        {
            BanManager.BanPlayer(account.ID, "Sistem", string.IsNullOrEmpty(sebep) ? "Konsol üzerinden yasaklandı" : sebep, true);
            Console.WriteLine($"{account.Username} banlandı.");
        }
    }

    private static void Unban(string input)
    {
        var account = ResolveAccount(input);
        if (account != null)
        {
            BanManager.UnbanPlayer(account.ID, "Sistem", "Konsol üzerinden kaldırıldı");
            Console.WriteLine($"{account.Username} banı kaldırıldı.");
        }
    }

    private static void ProfileShow(string input)
    {
        var account = ResolveAccount(input);
        if (account != null)
        {
            if (SessionManager.IsOnline(account.ID))
            {
                Session session = SessionManager.GetSession(account.ID);
                ShowProfileHandler.test(session);
                Console.WriteLine($"{account.Username} için profil testi çalıştırıldı.");
            }
            else Console.WriteLine("Oyuncu online değil.");
        }
    }
}
