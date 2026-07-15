using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

public static class Banner
{
    private static int logStartLine = 15;
    private static readonly object consoleLock = new object();
    private static DateTime startTime = DateTime.Now;

    public static void Initialize()
    {
        startTime = DateTime.Now;

        // Konsol ayarları
        try
        {
            Console.Title = "🎮 Game Server v1.0";
        }
        catch { }
        try
        {
            Console.WindowWidth = 120;
            Console.WindowHeight = 35;
            Console.BufferHeight = 2000;
        }
        catch
        { /* Bazı sistemlerde çalışmayabilir */
        }

        try
        {
            Console.CursorVisible = false;
        }
        catch { }
        Console.OutputEncoding = Encoding.UTF8;
    }

    public static void ShowSplashScreen()
    {
        try
        {
            Console.Clear();
        }
        catch { }
        Console.ForegroundColor = ConsoleColor.Cyan;

        string[] asciiArt =
        {
            @"",
            @"        ╔══════════════════════════════════════════════════════════════════╗",
            @"        ║                                                                  ║",
            @"        ║     ██████╗  █████╗ ███╗   ███╗███████╗    ███████╗███████╗     ║",
            @"        ║    ██╔════╝ ██╔══██╗████╗ ████║██╔════╝    ██╔════╝██╔════╝     ║",
            @"        ║    ██║  ███╗███████║██╔████╔██║█████╗      ███████╗███████╗     ║",
            @"        ║    ██║   ██║██╔══██║██║╚██╔╝██║██╔══╝      ╚════██║╚════██║     ║",
            @"        ║    ╚██████╔╝██║  ██║██║ ╚═╝ ██║███████╗    ███████║███████║     ║",
            @"        ║     ╚═════╝ ╚═╝  ╚═╝╚═╝     ╚═╝╚══════╝    ╚══════╝╚══════╝     ║",
            @"        ║                                                                  ║",
            @"        ║                   S E R V E R   v 1 . 0 . 0                     ║",
            @"        ║                                                                  ║",
            @"        ╚══════════════════════════════════════════════════════════════════╝",
        };

        foreach (string line in asciiArt)
        {
            Console.WriteLine(line);
            Thread.Sleep(50);
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n                   🌐 Multiplayer Game Server Platform");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("                   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\n                  Başlatılıyor... Lütfen bekleyin.\n");

        Console.ResetColor();
        Thread.Sleep(1200);
    }

    public static void ShowLoadingAnimation(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"\n        {message}");

        string[] spinner = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        int counter = 0;

        DateTime endTime = DateTime.Now.AddSeconds(1.5);
        while (DateTime.Now < endTime)
        {
            Console.Write($"\r        {message} {spinner[counter % spinner.Length]}");
            counter++;
            Thread.Sleep(80);
        }

        Console.WriteLine();
        Console.ResetColor();
    }

    public static void ShowLoadingStep(string message, int step, int total)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"\n        [{step}/{total}] ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(message);

        for (int i = 0; i < 3; i++)
        {
            Console.Write(".");
            Thread.Sleep(150);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" ✓");
        Console.ResetColor();
        Thread.Sleep(250);
    }

    public static void ShowSuccess(string message)
    {
        ClearLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ {message}");
        Console.ResetColor();
        Thread.Sleep(800);
    }

    public static void DrawMainInterface()
    {
        lock (consoleLock)
        {
            try
            {
                Console.Clear();
            }
            catch { }
            DrawHeader();
            DrawStatsPanel();
            DrawLogPanel();
            DrawCommandPanel();
        }
    }

    private static void DrawHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(
            "╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗"
        );
        Console.WriteLine(
            "║                                           🎮 GAME SERVER CONSOLE                                               ║"
        );
        Console.WriteLine(
            "╠════════════════════════════════════════════════════════════════════════════════════════════════════════════════╣"
        );
        Console.ResetColor();
    }

    private static void DrawStatsPanel()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(
            "║  📊 SERVER STATISTICS                                                                                          ║"
        );
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(
            "║  ──────────────────────────────────────────────────────────────────────────────────────────────────────────  ║"
        );
        Console.ResetColor();

        string uptime = GetUptime();
        int sessions = SessionManager.GetCount();
        string memory = GetMemoryUsage();
        string cpu = GetCpuUsage();
        int accounts = AccountCache.Count();
        int clubs = ClubCache.Count();

        WriteStatLine("🕐 Uptime", uptime, ConsoleColor.Cyan);
        WriteStatLine(
            "📡 Connections",
            $"{sessions} active",
            sessions > 0 ? ConsoleColor.Green : ConsoleColor.Gray
        );
        WriteStatLine("💾 Memory", memory, ConsoleColor.Magenta);
        WriteStatLine("🖥️  CPU", cpu, ConsoleColor.Yellow);
        WriteStatLine("👥 Accounts", accounts.ToString(), ConsoleColor.White);
        WriteStatLine("🏆 Clubs", clubs.ToString(), ConsoleColor.White);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(
            "╠════════════════════════════════════════════════════════════════════════════════════════════════════════════════╣"
        );
        Console.ResetColor();
    }

    private static void WriteStatLine(string icon, string value, ConsoleColor color)
    {
        Console.Write("║  ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{icon, -18}");
        Console.ForegroundColor = color;
        Console.Write($"{value, -93}");
        Console.ResetColor();
        Console.WriteLine("║");
    }

    private static void DrawLogPanel()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(
            "║  📋 SERVER LOGS                                                                                                ║"
        );
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(
            "║  ──────────────────────────────────────────────────────────────────────────────────────────────────────────  ║"
        );
        Console.ResetColor();

        // Log alanı (10 satır)
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine(
                "║                                                                                                            ║"
            );
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(
            "╠════════════════════════════════════════════════════════════════════════════════════════════════════════════════╣"
        );
        Console.ResetColor();
    }

    private static void DrawCommandPanel()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(
            "║  💡 Commands: help | stats | save | clear | restart | shutdown                                                ║"
        );
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(
            "╠════════════════════════════════════════════════════════════════════════════════════════════════════════════════╣"
        );
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("║  server > ");
        Console.ResetColor();
        Console.WriteLine(
            "                                                                                                        ║"
        );

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(
            "╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝"
        );
        Console.ResetColor();
    }

    public static void AddLog(string message, ConsoleColor color = ConsoleColor.Gray)
    {
        lock (consoleLock)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logMessage = $"[{timestamp}] {message}";

                // Log satırlarını kaydır
                for (int i = logStartLine; i < logStartLine + 9; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write("║  ");
                    Console.Write(new string(' ', 110));
                    Console.WriteLine("║");
                }

                // En alta yeni logu yaz
                Console.SetCursorPosition(0, logStartLine + 9);
                Console.Write("║  ");
                Console.ForegroundColor = color;
                Console.Write(
                    logMessage.Length > 110
                        ? logMessage.Substring(0, 107) + "..."
                        : logMessage.PadRight(110)
                );
                Console.ResetColor();
                Console.WriteLine("║");

                // Cursor'u komut satırına geri getir
                Console.SetCursorPosition(12, 29);
            }
            catch { }
        }
    }

    public static void UpdateStats()
    {
        lock (consoleLock)
        {
            try
            {
                Console.SetCursorPosition(0, 5);

                string uptime = GetUptime();
                int sessions = SessionManager.GetCount();
                string memory = GetMemoryUsage();
                string cpu = GetCpuUsage();
                int accounts = AccountCache.Count();
                int clubs = ClubCache.Count();

                WriteStatLine("🕐 Uptime", uptime, ConsoleColor.Cyan);
                WriteStatLine(
                    "📡 Connections",
                    $"{sessions} active",
                    sessions > 0 ? ConsoleColor.Green : ConsoleColor.Gray
                );
                WriteStatLine("💾 Memory", memory, ConsoleColor.Magenta);
                WriteStatLine("🖥️  CPU", cpu, ConsoleColor.Yellow);
                WriteStatLine("👥 Accounts", accounts.ToString(), ConsoleColor.White);
                WriteStatLine("🏆 Clubs", clubs.ToString(), ConsoleColor.White);

                Console.SetCursorPosition(12, 29);
            }
            catch { }
        }
    }

    public static void ShowShutdownScreen()
    {
        try
        {
            Console.Clear();
        }
        catch { }
        try
        {
            Console.CursorVisible = false;
        }
        catch { }

        Console.ForegroundColor = ConsoleColor.Yellow;

        string[] shutdownArt =
        {
            @"",
            @"        ╔══════════════════════════════════════════════════════════════════╗",
            @"        ║                                                                  ║",
            @"        ║                      🔴 SUNUCU KAPATILIYOR                       ║",
            @"        ║                                                                  ║",
            @"        ╚══════════════════════════════════════════════════════════════════╝",
            @"",
        };

        foreach (string line in shutdownArt)
        {
            Console.WriteLine(line);
        }

        string[] steps =
        {
            "Aktif bağlantılar sonlandırılıyor",
            "Oyuncu verileri kaydediliyor",
            "Kulüp verileri kaydediliyor",
            "Market verileri kaydediliyor",
            "Thread'ler durduruluyor",
            "Cache'ler temizleniyor",
        };

        for (int i = 0; i < steps.Length; i++)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"        [{i + 1}/{steps.Length}] {steps[i]}");

            string[] dots = { ".  ", ".. ", "..." };
            for (int j = 0; j < 3; j++)
            {
                Console.Write($"\r        [{i + 1}/{steps.Length}] {steps[i]}{dots[j]}");
                Thread.Sleep(200);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" ✓");
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("        ✅ Sunucu başarıyla kapatıldı!");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"        ⏱️  Toplam çalışma süresi: {GetUptime()}");
        Console.ResetColor();

        Thread.Sleep(2500);
    }

    public static void ShowErrorScreen(Exception ex)
    {
        try
        {
            Console.Clear();
        }
        catch { }
        Console.ForegroundColor = ConsoleColor.Red;

        string[] errorArt =
        {
            @"",
            @"        ╔══════════════════════════════════════════════════════════════════╗",
            @"        ║                                                                  ║",
            @"        ║                    ⚠️  KRİTİK HATA OLUŞTU!                      ║",
            @"        ║                                                                  ║",
            @"        ╚══════════════════════════════════════════════════════════════════╝",
            @"",
        };

        foreach (string line in errorArt)
        {
            Console.WriteLine(line);
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"        Hata Mesajı: {ex.Message}");
        Console.WriteLine($"        Hata Türü: {ex.GetType().Name}");
        Console.WriteLine($"\n        Stack Trace:");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"        {ex.StackTrace}");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n        Sunucu 10 saniye içinde kapatılacak...");
        Console.ResetColor();

        Thread.Sleep(10000);
    }

    public static void ShowHelp()
    {
        AddLog("📖 Komut listesi gösteriliyor...", ConsoleColor.Cyan);
        Thread.Sleep(500);

        AddLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.DarkGray);
        AddLog("help        - Yardım menüsünü gösterir", ConsoleColor.White);
        AddLog("stats       - Detaylı istatistikleri gösterir", ConsoleColor.White);
        AddLog("save        - Tüm verileri kaydeder", ConsoleColor.White);
        AddLog("clear       - Ekranı temizler", ConsoleColor.White);
        AddLog("restart     - Sunucuyu yeniden başlatır", ConsoleColor.White);
        AddLog("shutdown    - Sunucuyu kapatır", ConsoleColor.White);
        AddLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.DarkGray);
    }

    public static void ShowDetailedStats()
    {
        AddLog("📈 Detaylı istatistikler:", ConsoleColor.Cyan);
        Thread.Sleep(300);
        AddLog($"   Çalışma Süresi: {GetUptime()}", ConsoleColor.White);
        AddLog($"   Aktif Bağlantılar: {SessionManager.Count()}", ConsoleColor.White);
        AddLog($"   Bellek Kullanımı: {GetMemoryUsage()}", ConsoleColor.White);
        AddLog($"   CPU Kullanımı: {GetCpuUsage()}", ConsoleColor.White);
        AddLog($"   Toplam Hesap: {AccountCache.GetCachedAccounts()}", ConsoleColor.White);
        AddLog($"   Toplam Kulüp: {ClubCache.GetCachedClubs()}", ConsoleColor.White);
    }

    private static string GetUptime()
    {
        TimeSpan uptime = DateTime.Now - startTime;
        return $"{uptime.Days}d {uptime.Hours:D2}h {uptime.Minutes:D2}m {uptime.Seconds:D2}s";
    }

    private static string GetMemoryUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            long memoryMB = process.WorkingSet64 / (1024 * 1024);
            return $"{memoryMB} MB";
        }
        catch
        {
            return "N/A";
        }
    }

    private static string GetCpuUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;

            Thread.Sleep(500);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;

            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            return $"{cpuUsageTotal * 100:F1}%";
        }
        catch
        {
            return "N/A";
        }
    }

    private static void ClearLine()
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, Console.CursorTop - 1);
    }
}
