[HttpController]
public class MatchController : BaseController
{
    [HttpRoute("GET", "/api/matches")]
    public object GetMatches()
    {
        var activeBattles = ArenaManager.GetAllBattles()
            .Where(b => b.State == BattleState.Active)
            .OrderByDescending(b => b.GetPlayers().Count)
            .ThenByDescending(b => b.BattleId)
            .ToList();

        var battles = activeBattles.Select(b =>
        {
            var players = b.GetPlayers()
                .OrderByDescending(p => p.IsAlive)
                .ThenBy(p => p.Username ?? string.Empty)
                .Select(p =>
                {
                    var account = p.session?.Account ?? AccountCache.Load(p.ID);
                    return new
                    {
                        id = p.ID,
                        username = p.Username,
                        isAlive = p.IsAlive,
                        health = p.Health,
                        battleId = p.BattleId,
                        rotation = p.Rotation,
                        selectedSlot = p.SelectedSlot,
                        lastProcessedTick = p.LastProcessedTick,
                        connected = p.session != null && p.session.IsConnected,
                        position = new { x = p.Position.x, y = p.Position.y, z = p.Position.z },
                        account = account == null ? null : new
                        {
                            level = account.Level,
                            trophies = account.Trophy,
                            gems = account.Gems,
                            coins = account.Coins,
                            avatarId = account.Avatarid,
                            clubId = account.Clubid,
                            clubName = account.ClubName
                        }
                    };
                }).ToList();

            return new
            {
                battleId = b.BattleId,
                state = b.State.ToString(),
                startedAt = b.StartedAt == DateTime.MinValue ? null : b.StartedAt.ToString("o"),
                elapsedSeconds = b.StartedAt == DateTime.MinValue ? 0 : (DateTime.Now - b.StartedAt).TotalSeconds,
                playerCount = players.Count,
                aliveCount = players.Count(p => p.isAlive),
                players = players
            };
        }).ToList();

        return new
        {
            totalBattles = battles.Count,
            totalPlayers = battles.Sum(b => b.playerCount),
            battles = battles
        };
    }
}
