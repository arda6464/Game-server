[HttpController]
public class MatchController : BaseController
{
    [HttpRoute("GET", "/api/matches")]
    public object GetMatches()
    {
        var battleServer = GameServer.BattleServer;
        if (battleServer == null)
        {
            return new { totalBattles = 0, totalPlayers = 0, battles = new List<object>() };
        }

        var battles = new List<object>
        {
            new
            {
                battleId = 1,
                state = "Active",
                playerCount = battleServer.PlayerCount,
            }
        };

        return new
        {
            totalBattles = battles.Count,
            totalPlayers = 0,
            battles = battles
        };
    }
}
