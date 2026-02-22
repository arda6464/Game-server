using System.Numerics;

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
    private static readonly Dictionary<int, Battle> battles = new();
    private static int nextBattleId = 1;
    private static readonly object _lock = new object();

    public static int CreateBattle()
    {
        lock (_lock)
        {
            int id = nextBattleId++;
            var battle = new Battle { BattleId = id };
            battles[id] = battle;
            Logger.battlelog($"Yeni savaş oluşturuldu id: {id}");
            return id;
        }
    }

    public static void RemoveBattle(int battleId)
    {
        lock (_lock)
        {
            if (battles.Remove(battleId))
            {
                Logger.battlelog($"Savaş silindi id: {battleId}");
            }
        }
    }

    public static List<Battle> GetAllBattles()
    {
        lock (_lock)
        {
            return battles.Values.ToList();
        }
    }

    public static Battle? GetBattle(int battleId)
    {
        lock (_lock)
        {
            battles.TryGetValue(battleId, out var battle);
            return battle;
        }
    }
}
