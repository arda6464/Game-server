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
            Console.WriteLine("\n[Shutdown] Ctrl+C algılandı. Graceful shutdown başlatılıyor...");
            e.Cancel = true; // Process'i hemen kill etme
            Shutdown();
        };
        
        // Domain unload event
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Shutdown();
        
       
        
        // Cache'leri başlat
        AccountCache.Init();
        ClubCache.Init();
        
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
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Program] Ana thread hatası: {ex}");
        }
        finally
        {
            Shutdown();
        }
    }
    
    static void Shutdown()
    {
        Logger.genellog("[Program] Graceful shutdown başlatılıyor...");
        
        try
        {
            // GameServer'ı durdur
            gameserver?.Stop();
            
            // Thread'leri durdur (zaten Stop metodunda durdurulacaklar)
            Thread.Sleep(1000); // Thread'lerin kapanması için bekle
            
            Logger.genellog("[Program] Shutdown tamamlandı!");
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Program] Shutdown hatası: {ex.Message}");
        }
        
        Environment.Exit(0);
    }
}