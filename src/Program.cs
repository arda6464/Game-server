using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static GameServer? gameserver;
    static Thread? cmdhandlerthread;
    static Thread? pingthread;
    static Thread? botthread;
    static AdminServer? adminServer;

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
        DatabaseManager.Initialize();

        AccountCache.Init();
        ClubCache.Init();
        BanManager.Init();
        ShopManager.InitializeMarket();
        TicketStorage.Initialize();
        AndroidNotficationManager.Initialize();
        ReportManager.Init();
        MessageManager.Init(); // Packet Handler'larını yükle


        // Thread'leri başlat
        botthread = new Thread(() => bot.Start());
        botthread.Start();
        cmdhandlerthread = new Thread(Cmdhandler.Start);
        cmdhandlerthread.Start();

        pingthread = new Thread(() => SessionManager.PingManager(true));
        pingthread.Start();

        int publicPort = Config.Instance.Port;

        adminServer = new AdminServer();
        adminServer.Start();

        gameserver = new GameServer();
        gameserver.Start(publicPort); // Sadece UDP'yi başlatacak

        Console.WriteLine($"Sunucu {Config.Instance.ServerVersion} sürümünde!");
        ScheduleManager.Init();
        TickManager tickManager = new TickManager(30);

        try
        {
       //     tickManager.Start();

            // Ana TCP Dinleyicisi (Tek Port)
            TcpListener listener = new TcpListener(IPAddress.Any, publicPort);
            listener.Start();
            Logger.genellog($"[MULTIPLEXER] Tek port üzerinden dinleniyor: {publicPort}");

            Console.WriteLine("[Program] Server çalışıyor. Çıkmak için Ctrl+C'ye basın...");

            while (true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            NetworkStream stream = client.GetStream();
                            byte[] buffer = new byte[1024];
                            int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (read <= 0) return;

                            string initialData = Encoding.ASCII.GetString(buffer, 0, read);
                            bool isHttp = initialData.StartsWith("GET ") ||
                                          initialData.StartsWith("POST ") ||
                                          initialData.StartsWith("OPTIONS ") ||
                                          initialData.StartsWith("HEAD ") ||
                                          initialData.StartsWith("PUT ") ||
                                          initialData.StartsWith("DELETE ");

                            if (isHttp)
                            {
                                // Kalan veriyi de içerecek şekilde byte array oluştur
                                byte[] data = new byte[read];
                                Array.Copy(buffer, 0, data, 0, read);
                                adminServer.HandleConnection(client, data);
                            }
                            else
                            {
                                // Oyun verisi
                                byte[] data = new byte[read];
                                Array.Copy(buffer, 0, data, 0, read);
                                gameserver.HandleConnection(client, data);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.errorslog($"[Multiplexer] Bağlantı hatası: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.errorslog($"[Multiplexer] Accept hatası: {ex.Message}");
                }
            }
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
            BanManager.Stop();
            TicketStorage.SaveAllData(BotManager.istance.TicketSystem.tickets, BotManager.istance.TicketSystem.channelToTicket);
            TickManager.instance.Stop();
            ScheduleManager.Stop();
            adminServer?.Stop();

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