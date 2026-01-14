using System;
using System.IO;
using System.Threading;

public class Logger
{
    private static readonly string erorlogerpath = "erors.txt";
    private static readonly string accountlogpath = "accountslog.txt";
    private static readonly string battleslogpath = "battleslog.txt";
    private static readonly string genellogpath = "genellog.txt";
    private static readonly object fileLock = new object();
    private static readonly int maxRetryCount = 3;
    private static readonly int retryDelayMs = 100;

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

    private static void SafeLog(string mesaj, string filePath, ConsoleColor color, string logType)
    {
        DateTime saat = DateTime.Now;
        string logMessage = $"[{saat:yyyy-MM-dd HH:mm:ss}] [{logType}] {mesaj}";

        // Console'a yaz
        WriteToConsole(logMessage, color);

        // File'a yaz (thread-safe ve retry mekanizmalı)
        WriteToFileWithRetry(logMessage, filePath);
    }

    private static void WriteToConsole(string message, ConsoleColor color)
    {
        try
        {
            lock (fileLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            // Console yazma hatası - görmezden gel veya fallback yap
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
                return; // Başarılı oldu, çık
            }
            catch (IOException) when (attempt < maxRetryCount)
            {
                // Dosya kilitli - bekleyip tekrar dene
                Thread.Sleep(retryDelayMs * attempt);
            }
            catch (Exception ex)
            {
                // Diğer hatalar için fallback
                FallbackLog($"File write failed ({filePath}): {ex.Message}");
                return;
            }
        }

        // Tüm denemeler başarısız oldu
        FallbackLog($"All retries failed for: {filePath}");
    }

    private static void WriteToFile(string message, string filePath)
    {
        lock (fileLock)
        {
            // Dosya boyutu kontrolü ve rotation
            CheckFileSizeAndRotate(filePath);

            // FileShare.ReadWrite ile diğer process'lerin de erişimine izin ver
            using (var stream = new FileStream(
                filePath, 
                FileMode.Append, 
                FileAccess.Write, 
                FileShare.ReadWrite, 
                bufferSize: 4096, 
                useAsync: false))
            using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
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
            if (fileInfo.Exists && fileInfo.Length > 10 * 1024 * 1024) // 10MB
            {
                string backupPath = $"{Path.GetFileNameWithoutExtension(filePath)}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(filePath)}";
                File.Move(filePath, backupPath);
                
                // Eski backup'ları temizle (30 günden eski)
                CleanOldBackups(Path.GetDirectoryName(filePath) ?? ".", 
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
                if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-30)) // 30 günden eski
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
            // Acil durum log'u - her zaman çalışsın
            string fallbackPath = "emergency_log.txt";
            string fallbackMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [FALLBACK] {message}";

            using (var stream = new FileStream(
                fallbackPath, 
                FileMode.Append, 
                FileAccess.Write, 
                FileShare.ReadWrite))
            using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
            {
                writer.WriteLine(fallbackMessage);
            }

            // Console'a da yazmaya çalış
            Console.WriteLine(fallbackMessage);
        }
        catch
        {
            // Son çare - hiçbir şey yapma, exception fırlatma
        }
    }

    // Logger'ı temiz kapatmak için
    public static void FlushAllLogs()
    {
        // Gerekli temizlik işlemleri buraya eklenebilir
        Console.WriteLine("Logger flushing completed.");
    }
}