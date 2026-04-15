using System;
using System.Threading;
using System.Threading.Tasks;

public static class ScheduleManager
{
    private static bool _isRunning;
    private static CancellationTokenSource _cts = new CancellationTokenSource();

    public static void Init()
    {
        if (_isRunning) return;
        _isRunning = true;
        
        // Start the background loop
        Task.Run(() => Loop(_cts.Token));
        
        Logger.genellog("[ScheduleManager] Zamanlayıcı sistemi başlatıldı.");
    }

    public static void Stop()
    {
        _isRunning = false;
        _cts.Cancel();
        Logger.genellog("[ScheduleManager] Zamanlayıcı sistemi durduruldu.");
    }

    private static async Task Loop(CancellationToken token)
    {
        int lastMinute = -1;
        int lastHour = -1;

        while (!token.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;

                // Minute Tick (Her dakika bir kez)
                if (now.Minute != lastMinute)
                {
                    lastMinute = now.Minute;
                    
                    // Doğrudan çağrı yapıyoruz (Event bazlı değil)
                    DynamicConfigManager.ProcessEvents();
                }

                // Hour Tick (Her saat bir kez)
                if (now.Hour != lastHour)
                {
                    lastHour = now.Hour;
                    // Gelecekte buraya saatlik görevler eklenebilir
                }

                // Check again in a few seconds (Oversampling to avoid missing a minute)
                await Task.Delay(5000, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[ScheduleManager] Beklenmedik hata: {ex.Message}");
                await Task.Delay(10000, token); // Error recovery delay
            }
        }
    }
}
