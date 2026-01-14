using System;
using System.Text;
using System.Threading;
    using System.Drawing;

class Program
{
    static GameServer? gameserver;
    static Thread? cmdhandlerthread;
    static Thread? pingthread;
    static Thread? botthread;
  
    static void Main()
    {
        Console.Clear();
         Colorful.Console.WriteWithGradient(
                @"
    _____        ____ __ ______    _____ ______ _______      ________ _____  
  / ____|   /\   |  \/  |  ____|  / ____|  ____|  __ \ \    / /  ____|  __ \ 
 | |  __   /  \  | \  / | |__    | (___ | |__  | |__) \ \  / /| |__  | |__) |
 | | |_ | / /\ \ | |\/| |  __|    \___ \|  __| |  _  / \ \/ / |  __| |  _  / 
 | |__| |/ ____ \| |  | | |____   ____) | |____| | \ \  \  /  | |____| | \ \ 
  \_____/_/    \_\_|  |_|______| |_____/|______|_|  \_\  \/   |______|_|  \_\
                                                                             
   _____  __  ___   ___  ___  ___  ____ ____
  / _ ) \/ / / _ | / _ \/ _ \/ _ |/ __// / /
 / _  |\  / / __ |/ , _/ // / __ / _ \/_  _/
/____/ /_/ /_/ |_/_/|_/____/_/ |_\___/ /_/                                                                                                                                                                                
       " + "\n\n", Color.Fuchsia, Color.Cyan, 8);
        // Graceful shutdown handler
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\n[Shutdown] Ctrl+C algılandı. Veriler kaydediliyor...");
            e.Cancel = true; // Process'i hemen kill etme

            // Hemen kaydet ve çık
            SaveDataAndExit();
        };

        // Cache'leri başlat
        BotManager bot = new BotManager();
        Config.Load("config.json");
       
        AccountCache.Init();
        ClubCache.Init();
        BanManager.Init();
        ShopManager.InitializeMarket();
        TicketStorage.Initialize();

        // Thread'leri başlat
        botthread = new Thread(() => bot.Start());
        botthread.Start();
        cmdhandlerthread = new Thread(Cmdhandler.Start);
        cmdhandlerthread.Start();
        
        pingthread = new Thread(() => SessionManager.PingManager(true));
        pingthread.Start();
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        gameserver = new GameServer();
        Console.WriteLine($"Sunucu {Config.Instance.ServerVersion} sürümünde!");
        TickManager tickManager = new TickManager(20); 
        
        try
        {
            tickManager.Start();
            gameserver.Start(Config.Instance.Port);
            
            
            Console.WriteLine("[Program] Server çalışıyor. Çıkmak için Ctrl+C'ye basın...");
            Thread.Sleep(Timeout.Infinite); // Sonsuz bekle
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Program] Ana thread hatası: {ex}");
        }
        finally
        {
            SaveDataAndExit();
        }
    }
    
    static void SaveDataAndExit()
    {
        Logger.genellog("[Program] Veriler kaydediliyor...");
        
        try
        {
            // Config watcher'ı durdur
            Config.StopWatcher();
            
            Maintance.StartMaintance(TimeSpan.FromHours(3), true);
            // Sadece dataları kaydet
            AccountCache.SaveAll();
            ClubCache.SaveAll();
            TicketStorage.SaveAllData(BotManager.istance.TicketSystem.tickets,BotManager.istance.TicketSystem.channelToAccount);
            TickManager.instance.Stop();
            
            Logger.genellog("[Program] Veriler kaydedildi, program kapatılıyor!");
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Program] Veri kaydetme hatası: {ex.Message}");
        }
        
        // Hemen çık
        Environment.Exit(0);
    }
}