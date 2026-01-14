using System.Numerics;

public class Arena
{
    public int ArenaId { get; set; }
    public int BulletId = 0;
    public List<Player> Players { get; set; } = new List<Player>();
    public List<Bullet> Bullets { get; set; } = new List<Bullet>();
      private readonly object _lock = new object();

    public void AddBullet(Bullet bullet)
    {
        lock (_lock)
        {
            Bullets.Add(bullet);
        }
       // Console.WriteLine($"[ARENA {ArenaId}] Yeni mermi eklendi: {bullet.BulletId} sahibi: {bullet.OwnerId} (Toplam: {Bullets.Count})");
    }
    public int GetBulletId()
    {   
        BulletId++;
    return BulletId; 
    }


    public void RemoveBullet(int bulletId)
    {
        lock (_lock)
        {
            Bullets.RemoveAll(b => b.BulletId == bulletId);
        }
    }

    public void UpdateBullets()
    {
        lock (_lock)
        {
            float currentTime = GetCurrentTime();
            foreach (var bullet in Bullets.ToList())
            {
                if (bullet.IsActive)
                {

                    bullet.Position += bullet.Direction * bullet.Speed;
                    //  Console.WriteLine($"[ARENA {ArenaId}] Mermi {bullet.BulletId} pozisyonu güncellendi: ({bullet.Position.X}, {bullet.Position.Y})");
                    float traveledDistance = Vector2.Distance(bullet.startPos, bullet.Position);
                    if (traveledDistance >= bullet.menzil)
                    {
                        bullet.IsActive = false; // Pasif yap
                        bullet.DeathTime = currentTime;

                       // Console.WriteLine($"[ARENA {ArenaId}] Mermi {bullet.BulletId} pasif edildi");
                    }
                }


                if (!bullet.IsActive && (currentTime - bullet.DeathTime) > 3.0f)
                {

                    Bullets.Remove(bullet);
                 //   Console.WriteLine($"[ARENA {ArenaId}] Mermi {bullet.BulletId} bellekten silindi");
                }
            
            }
        }
    }
    private float GetCurrentTime()
{
    return Environment.TickCount / 1000f;
}

    public Bullet GetBullet(int id)
    {
        if (Bullets == null) return null;
        lock (_lock)
        {
            return Bullets.FirstOrDefault(b => b.BulletId == id);
        }
    }
    public Player? GetPlayer(string accountId)
    {
        lock (_lock)
        {
            return Players.FirstOrDefault(p => p.AccountId == accountId);
        }
    }
    public void UpdatePlayerPosition(string accountId, Vector2 newpos)
    {
        lock (_lock)
        {
            var player = Players.FirstOrDefault(p => p.AccountId == accountId);
            if (player != null)
            {
                player.Position = newpos;
                player.session.PlayerData.Position = newpos;
            }
           
        }
    }

    public List<Player> GetPlayers()
    {
        lock (_lock)
        {
            return Players;
        }
    }
    public void RemovePlayer(string accountId)
    {
        lock (_lock)
        {
            Players.RemoveAll(p => p.AccountId == accountId);
        }
    }
    public void AddPlayer(Player player)
    {
        lock (_lock)
        {
            Players.Add(player);
        }
    }

}
public class Bullet
{
    public int BulletId { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Direction { get; set; }
    public float Speed { get; set; }
    public string? OwnerId { get; set; }
    public int Damage { get; set; } = 40;
    public float menzil { get; set; } = 40f;
    public Vector2 startPos { get; set; }
    public bool IsActive { get; set; } = true; // Aktif mi?
    public float DeathTime { get; set; } = 0; // Ne zaman öldü?
}
            
public static class ArenaManager
{
   private static readonly Dictionary<int, Arena> arenas = new();

    private static int nextArenaId = 1;

   private static readonly object _lock = new object(); // Lock eklendi

    public static int CreateArena()
    {
        lock (_lock)
        {
            int id = nextArenaId++;
         arenas[id] = new Arena { ArenaId = id };
            Logger.battlelog($"Yeni harita oluşturuldu id: {id}");
            return id;
        }
    }
   

    public static List<Arena> GetAllArenas()
    {
        lock (_lock)
        {
            return arenas.Values.ToList();
        }
    }

   
    public static Arena? GetArena(int arenaId)
{
    lock (_lock)
    {
        arenas.TryGetValue(arenaId, out var arena);
        return arena;
    }
}
  
}