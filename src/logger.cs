using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File;

public class Logger
{
    private static readonly object fileLock = new object();
    private static readonly Stopwatch bootWatch = Stopwatch.StartNew();

    public static void Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: "Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        Log.Information("Logger başlatıldı.");
    }

    public void AccountLog(string mesaj)
    {
        Log.Information("ACCOUNT: {Message}", mesaj);
        Console.WriteLine($"[ACCOUNT] {mesaj}");
    }

    public static void errorslog(string mesaj)
    {
        Log.Error("ERROR: {Message}", mesaj);
        Console.WriteLine($"[ERROR] {mesaj}");
    }

    public static void battlelog(string mesaj)
    {
        Log.Information("BATTLE: {Message}", mesaj);
        Console.WriteLine($"[BATTLE] {mesaj}");
    }

    public static void genellog(string mesaj)
    {
        Log.Information("GENERAL: {Message}", mesaj);
        Console.WriteLine($"[GENERAL] {mesaj}");
    }

    public static void bootlog(string mesaj)
    {
        Log.Information("BOOT: {Message}", mesaj);
        Console.WriteLine($"[BOOT] {mesaj}");
    }

    public static void warnlog(string mesaj)
    {
        Log.Warning("WARN: {Message}", mesaj);
        Console.WriteLine($"[WARN] {mesaj}");
    }

    public static void successlog(string mesaj)
    {
        Log.Information("OK: {Message}", mesaj);
        Console.WriteLine($"[OK] {mesaj}");
    }

    private static void FallbackLog(string message)
    {
        try
        {
            string fallbackPath = "Logs/emergency_log.txt";
            string fallbackMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [FALLBACK] {message}";

            if (!Directory.Exists("Logs"))
                Directory.CreateDirectory("Logs");

            using (var stream = new FileStream(
                fallbackPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine(fallbackMessage);
            }

            Console.WriteLine(fallbackMessage);
        }
        catch
        {
        }
    }

    public static void FlushAllLogs()
    {
        var elapsed = bootWatch.Elapsed;
        Console.WriteLine($"Logger flushing completed after {elapsed.TotalSeconds:F1}s.");
    }
}
