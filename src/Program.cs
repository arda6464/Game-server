using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

static class Program
{
    private static readonly Stopwatch bootWatch = Stopwatch.StartNew();

    static GameServer? gameserver;
    static Thread? cmdhandlerthread;
    static Thread? pingthread;
    static Thread? botthread;
    static AdminServer? adminServer;
    static Multiplexer? multiplexer;
    static bool isShuttingDown = false;

    static void Main()
    {
        PrepareConsole();
        PrintBootHeader();
        Logger.Initialize();
        RegisterShutdownHandlers();

        try
        {
            BootStep("Map data", () => MapManager.Load("Data/MapData.json"));
            BootStep("Config", () => Config.Load("Data/config.json"));

            UpdateConsoleTitle();

            BotManager bot = new BotManager();
            BootStep("Core systems", () =>
            {
                DataManager.Init();
                DatabaseManager.Initialize();
                SeasonManager.Load();
            });

            BootStep("Caches", () =>
            {
                AccountCache.Init();
                ClubCache.Init();
                BanManager.Init();
                ShopManager.InitializeMarket();
                TicketStorage.Initialize();
                AndroidNotificationManager.Initialize();
                ReportManager.Init();
                MessageManager.Init();
                GachaSystem.GachaManager.Init();
                UpdateNotesManager.Init();
            });

            BootStep("Background workers", () =>
            {
                botthread = new Thread(() => bot.Start().GetAwaiter().GetResult())
                {
                    IsBackground = true,
                    Name = "DiscordBot"
                };
                botthread.Start();

                cmdhandlerthread = new Thread(CmdHandler.Start)
                {
                    IsBackground = true,
                    Name = "CommandHandler"
                };
                cmdhandlerthread.Start();

                pingthread = new Thread(() => SessionManager.PingManager(true))
                {
                    IsBackground = true,
                    Name = "PingManager"
                };
                pingthread.Start();
            });

            int publicPort = Config.Instance?.Port ?? 5000;

            BootStep("Admin + game services", () =>
            {
                adminServer = new AdminServer();
                adminServer.Start();
                HttpRouter.RegisterControllers();

                gameserver = new GameServer();
                gameserver.Start(publicPort);

                ScheduleManager.Init();
            });

            TickManager tickManager = new TickManager(30);
            BootStep("Tick loop", () => tickManager.Start());

            BootStep("Transport gateway", () =>
            {
                multiplexer = new Multiplexer(publicPort, adminServer!, gameserver!);
                multiplexer.Start();
            });

            Logger.successlog($"Server ready in {bootWatch.Elapsed.TotalSeconds:F1}s on port {publicPort}.");
            Logger.bootlog($"Version {Config.Instance?.ServerVersion ?? "unknown"} online.");
            Console.WriteLine("Press Ctrl+C to begin a graceful shutdown.");

            while (!isShuttingDown) Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Program] Fatal startup error: {ex}");
        }
        finally
        {
            SaveDataAndExit();
        }
    }

    private static void PrepareConsole()
    {
        try { Console.Clear(); } catch { }
        try { Console.CursorVisible = false; } catch { }
        Console.OutputEncoding = Encoding.UTF8;
        try { Console.Title = "Game Server Boot Sequence"; } catch { }
    }

    private static void PrintBootHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==============================================================");
        Console.WriteLine("                    GAME SERVER CORE");
        Console.WriteLine("==============================================================");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Layer 7 gateway, UDP combat, admin panel and background jobs");
        Console.WriteLine($"  Boot started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void UpdateConsoleTitle()
    {
        var version = Config.Instance?.ServerVersion ?? "unknown";
        var port = Config.Instance?.Port ?? 5000;
        try { Console.Title = $"Game Server {version} | Port {port}"; } catch { }
    }

    private static void RegisterShutdownHandlers()
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Logger.warnlog("Ctrl+C detected. Starting graceful shutdown.");
            SaveDataAndExit();
        };
    }

    private static void BootStep(string title, Action action)
    {
        var stepWatch = Stopwatch.StartNew();

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"  > {title,-24}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(" ... ");
        Console.ResetColor();

        try
        {
            action();
            stepWatch.Stop();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"ok ({stepWatch.ElapsedMilliseconds} ms)");
        }
        catch (Exception ex)
        {
            stepWatch.Stop();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"fail ({stepWatch.ElapsedMilliseconds} ms)");
            Console.ResetColor();

            Logger.errorslog($"[BOOT] {title} failed: {ex.Message}");
            throw;
        }
        finally
        {
            Console.ResetColor();
        }
    }

    public static void SaveDataAndExit()
    {
        isShuttingDown = true;
        Logger.bootlog("Saving data and stopping services.");

        try
        {
            Config.StopWatcher();
            Maintenance.StartMaintenance(TimeSpan.FromHours(3), true);

            AccountCache.SaveAll();
            ClubCache.SaveAll();
            BanManager.Stop();
            TicketStorage.SaveAllData(BotManager.istance.TicketSystem.tickets, BotManager.istance.TicketSystem.channelToTicket);
            TickManager.instance?.Stop();
            ScheduleManager.Stop();
            adminServer?.Stop();
            multiplexer?.Stop();
            gameserver?.Stop();

            Logger.successlog("Shutdown complete. All data flushed.");
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Program] SaveDataAndExit error: {ex.Message}");
        }

        Environment.Exit(0);
    }
}
