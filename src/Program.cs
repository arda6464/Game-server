using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private static readonly Stopwatch bootWatch = Stopwatch.StartNew();

    static GameServer? gameserver;
    static Thread? cmdhandlerthread;
    static Thread? pingthread;
    static Thread? botthread;
    static AdminServer? adminServer;
    static TcpListener? tcpListener;

    static void Main()
    {
        PrepareConsole();
        PrintBootHeader();
        RegisterShutdownHandlers();

        try
        {
            BootStep("Map data", () => MapManager.Load("MapData.json"));
            BootStep("Config", () => Config.Load("config.json"));

            UpdateConsoleTitle();

            BotManager bot = new BotManager();
            BootStep("Core systems", () =>
            {
                DataManager.Init();
                DatabaseManager.Initialize();
            });

            BootStep("Caches", () =>
            {
                AccountCache.Init();
                ClubCache.Init();
                BanManager.Init();
                ShopManager.InitializeMarket();
                TicketStorage.Initialize();
                AndroidNotficationManager.Initialize();
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

                cmdhandlerthread = new Thread(Cmdhandler.Start)
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

                gameserver = new GameServer();
                gameserver.Start(publicPort);

                ScheduleManager.Init();
            });

            TickManager tickManager = new TickManager(30);
            BootStep("Tick loop", () => tickManager.Start());

            BootStep("Transport gateway", () =>
            {
                tcpListener = new TcpListener(IPAddress.Any, publicPort);
                tcpListener.Start();
            });

            Logger.successlog($"Server ready in {bootWatch.Elapsed.TotalSeconds:F1}s on port {publicPort}.");
            Logger.bootlog($"Version {Config.Instance?.ServerVersion ?? "unknown"} online.");
            Console.WriteLine("Press Ctrl+C to begin a graceful shutdown.");

            RunMultiplexerLoop(publicPort);
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
        Console.Clear();
        Console.CursorVisible = false;
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "Game Server Boot Sequence";
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
        Console.Title = $"Game Server {version} | Port {port}";
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

    private static void RunMultiplexerLoop(int publicPort)
    {
        Logger.genellog($"[MULTIPLEXER] Listening on port {publicPort}");

        while (true)
        {
            try
            {
                TcpClient client = tcpListener!.AcceptTcpClient();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        byte[] buffer = new byte[1024];
                        int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (read <= 0)
                        {
                            client.Close();
                            return;
                        }

                        string initialData = Encoding.ASCII.GetString(buffer, 0, read);
                        bool isHttp = initialData.StartsWith("GET ") ||
                                      initialData.StartsWith("POST ") ||
                                      initialData.StartsWith("OPTIONS ") ||
                                      initialData.StartsWith("HEAD ") ||
                                      initialData.StartsWith("PUT ") ||
                                      initialData.StartsWith("DELETE ");

                        byte[] data = new byte[read];
                        Array.Copy(buffer, 0, data, 0, read);

                        if (isHttp)
                        {
                            adminServer?.HandleConnection(client, data);
                        }
                        else
                        {
                            gameserver?.HandleConnection(client, data);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.errorslog($"[Multiplexer] Connection error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[Multiplexer] Accept error: {ex.Message}");
            }
        }
    }

    static void SaveDataAndExit()
    {
        Logger.bootlog("Saving data and stopping services.");

        try
        {
            Config.StopWatcher();
            Maintance.StartMaintance(TimeSpan.FromHours(3), true);

            AccountCache.SaveAll();
            ClubCache.SaveAll();
            BanManager.Stop();
            TicketStorage.SaveAllData(BotManager.istance.TicketSystem.tickets, BotManager.istance.TicketSystem.channelToTicket);
            TickManager.instance?.Stop();
            ScheduleManager.Stop();
            adminServer?.Stop();
            tcpListener?.Stop();
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
