public static class ArenaManager
{
    public static BattleServer? GetBattleServer() => GameServer.BattleServer;

    public static List<BattleServer> GetAllBattles()
    {
        var bs = GameServer.BattleServer;
        if (bs != null)
            return new List<BattleServer> { bs };
        return new List<BattleServer>();
    }
}
