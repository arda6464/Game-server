using System.Diagnostics;

public class TickManager
{
    public static TickManager instance;

    private Thread tickThread;
    private volatile bool isRunning;
    private uint tick;
    private int tickInterval;
    
    public int TickRate { get; private set; }
    public float DeltaTime { get; private set; }
    
    public TickManager(int tickRate = 30)
    {
        instance = this;
        this.TickRate = tickRate;
        this.DeltaTime = 1.0f / tickRate;

        tickInterval = 1000 / tickRate;
        tickThread = new Thread(Tick);
    }
    
    public void Start()
    {
        Console.WriteLine("[TICK] TickManager başlatılıyor...");
        isRunning = true;
        tickThread.Start();
    }
    
    public void Stop()
    {
        isRunning = false;
        Console.WriteLine("[TICK] TickManager durduruldu.");
    }
    
    private void Tick()
    {
        var stopwatch = Stopwatch.StartNew();
        long nextTickTime = stopwatch.ElapsedMilliseconds;

        while (isRunning)
        {
            nextTickTime += tickInterval;

            tick++;
            Time.Tick();
            Handle();

            long sleepTime = nextTickTime - stopwatch.ElapsedMilliseconds;
            if (sleepTime > 0)
            {
                Thread.Sleep((int)sleepTime);
            }
            else
            {
                Console.WriteLine($"[TICK] Tick gecikmesi: {-sleepTime}ms (Tick #{tick})");
            }
        }
    }
    
    private void Handle()
    {
        // Savaş güncellemeleri
        foreach (var battle in ArenaManager.GetAllBattles())
        {
            battle.Tick();
        }

        // Davet temizliği (Dakikada bir)
        if (tick % 1800 == 0)
        {
            InviteManager.Cleanup();
        }
    }

    
    public uint Get_Tick()
    {
        return this.tick;
    }

    public bool IsRunning()
    {
        return this.isRunning;
    }
}