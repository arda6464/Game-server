using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

public class Logger
{
    private static readonly string erorlogerpath = "Data/erors.txt";
    private static readonly string accountlogpath = "Data/accountslog.txt";
    private static readonly string battleslogpath = "Data/battleslog.txt";
    private static readonly string genellogpath = "Data/genellog.txt";
    private static readonly object fileLock = new object();
    private static readonly int maxRetryCount = 3;
    private static readonly int retryDelayMs = 100;
    private static readonly Stopwatch bootWatch = Stopwatch.StartNew();

    public void AccountLog(string mesaj)
    {
        SafeLog(mesaj, accountlogpath, ConsoleColor.DarkBlue, "ACCOUNT");
    }

    public static void errorslog(string mesaj)
    {
        SafeLog(mesaj, erorlogerpath, ConsoleColor.Red, "ERROR");
    }

    public static void battlelog(string mesaj)
    {
        SafeLog(mesaj, battleslogpath, ConsoleColor.Yellow, "BATTLE");
    }

    public static void genellog(string mesaj)
    {
        SafeLog(mesaj, genellogpath, ConsoleColor.Green, "GENERAL");
    }

    public static void bootlog(string mesaj)
    {
        SafeLog(mesaj, genellogpath, ConsoleColor.Cyan, "BOOT");
    }

    public static void warnlog(string mesaj)
    {
        SafeLog(mesaj, genellogpath, ConsoleColor.Yellow, "WARN");
    }

    public static void successlog(string mesaj)
    {
        SafeLog(mesaj, genellogpath, ConsoleColor.Green, "OK");
    }

    private static void SafeLog(string mesaj, string filePath, ConsoleColor color, string logType)
    {
        DateTime saat = DateTime.Now;
        string logMessage = $"[{saat:yyyy-MM-dd HH:mm:ss}] [{logType}] {mesaj}";

        WriteToConsole(saat, mesaj, color, logType);
        WriteToFileWithRetry(logMessage, filePath);
    }

    private static void WriteToConsole(DateTime timestamp, string message, ConsoleColor color, string logType)
    {
        try
        {
            lock (fileLock)
            {
                var previousColor = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[{timestamp:HH:mm:ss}] ");

                Console.ForegroundColor = color;
                Console.Write($"[{logType}] ");

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message);

                Console.ForegroundColor = previousColor;
            }
        }
        catch (Exception ex)
        {
            FallbackLog($"Console write failed: {ex.Message}");
        }
    }

    private static void WriteToFileWithRetry(string message, string filePath)
    {
        for (int attempt = 1; attempt <= maxRetryCount; attempt++)
        {
            try
            {
                WriteToFile(message, filePath);
                return;
            }
            catch (IOException) when (attempt < maxRetryCount)
            {
                Thread.Sleep(retryDelayMs * attempt);
            }
            catch (Exception ex)
            {
                FallbackLog($"File write failed ({filePath}): {ex.Message}");
                return;
            }
        }

        FallbackLog($"All retries failed for: {filePath}");
    }

    private static void WriteToFile(string message, string filePath)
    {
        lock (fileLock)
        {
            CheckFileSizeAndRotate(filePath);

            using (var stream = new FileStream(
                filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite,
                bufferSize: 4096,
                useAsync: false))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine(message);
            }
        }
    }

    private static void CheckFileSizeAndRotate(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists && fileInfo.Length > 10 * 1024 * 1024)
            {
                string backupPath = $"{Path.GetFileNameWithoutExtension(filePath)}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(filePath)}";
                File.Move(filePath, backupPath);

                CleanOldBackups(
                    Path.GetDirectoryName(filePath) ?? ".",
                    Path.GetFileNameWithoutExtension(filePath) + "_*" + Path.GetExtension(filePath));
            }
        }
        catch (Exception ex)
        {
            FallbackLog($"File rotation failed: {ex.Message}");
        }
    }

    private static void CleanOldBackups(string directory, string searchPattern)
    {
        try
        {
            var files = Directory.GetFiles(directory, searchPattern);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-30))
                {
                    fileInfo.Delete();
                }
            }
        }
        catch (Exception ex)
        {
            FallbackLog($"Backup cleanup failed: {ex.Message}");
        }
    }

    private static void FallbackLog(string message)
    {
        try
        {
            string fallbackPath = "Data/emergency_log.txt";
            string fallbackMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [FALLBACK] {message}";

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
