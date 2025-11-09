public static class ArenaManager
{
    private static readonly Dictionary<int, List<Player>> arenas = new();
    private static int nextArenaId = 1;

    public static int CreateArena()
    {
        int id = nextArenaId++;
        arenas[id] = new List<Player>();
        Logger.battlelog($"Yeni harita oluşturuldu id: {id}");
        return id;
    }
    public static void AddPlayer(int arenaid, Player player)
    {
        if (arenas.TryGetValue(arenaid, out var players))
        {
            players.Add(player);
            Logger.battlelog($"{player.Username} {arenaid} arenasına katıldı!");
        }
    }
    
    public static bool RemovePlayer(int arenaId, string accountId)
    {
        if (!arenas.TryGetValue(arenaId, out var players))
        {
            Logger.battlelog($"[ARENA] Uyarı: Arena {arenaId} bulunamadı!");
            return false;
        }

        var player = players.FirstOrDefault(p => p.AccountId == accountId);
        if (player != null)
        {
            players.Remove(player);
            Logger.battlelog($"[ARENA] {accountId} arenadan çıkarıldı. Kalan: {players.Count}");
            return true;
        }

        return false;
    }
    
    public static void RemoveArena(int arenaId)
    {
        if (arenas.Remove(arenaId))
        {
            Logger.battlelog($"[ARENA] Arena {arenaId} silindi.");
        }
    }
    
    public static List<Player> GetPlayers(int arenaId)
    {
        if (!arenas.TryGetValue(arenaId, out var players))
        {
            Logger.battlelog($"[ARENA] Uyarı: Arena {arenaId} bulunamadı!");
            return new List<Player>();
        }
        return players;
    }
}