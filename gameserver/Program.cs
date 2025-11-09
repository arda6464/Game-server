using System;
using System.Text;
using System.Threading;

class Program
{
    static GameServer? gameserver;
    static Thread? cmdhandlerthread;
    static Thread? pingthread;

    static void Main()
    {
        Console.Clear();
        
        // Graceful shutdown handler
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\n[Shutdown] Ctrl+C algılandı. Veriler kaydediliyor...");
            e.Cancel = true; // Process'i hemen kill etme
            
            // Hemen kaydet ve çık
            SaveDataAndExit();
        };
        
        // Cache'leri başlat
        AccountCache.Init();
        ClubCache.Init();
        BanManager.Init();
        
        // Thread'leri başlat
        cmdhandlerthread = new Thread(Cmdhandler.Start);
        cmdhandlerthread.Start();
        
        pingthread = new Thread(SessionManager.PingManager);
        pingthread.Start();
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        // GameServer'ı başlat
        gameserver = new GameServer();
        
        try
        {
            gameserver.Start();
            
            // Ana thread'i bekle
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
            // Sadece dataları kaydet
            AccountCache.SaveAll();
            ClubCache.SaveAll();
            
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