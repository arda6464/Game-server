public class TickManager
{
    public static TickManager instance;

    private Thread tickThread;
    private bool isRunning;
    private byte tick;
    private int tickInterval;
    
    public TickManager(int tickRate = 20)
    {
        instance = this;

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
        while (isRunning)
        {
            tick++;
            Time.Tick();
            Handle();
            Thread.Sleep(tickInterval);
        }
    }
    
    private void Handle()
    {
        // Savaş güncellemeleri
        foreach (var battle in ArenaManager.GetAllBattles())
        {
            battle.Tick();
        }
    }

    
    public byte Get_Tick()
    {
        return this.tick;
    }

    public bool IsRunning()
    {
        return this.isRunning;
    }
}