using Logic;

public static class MatchMaking
{
    private static readonly List<Session> waitingQueue = new();
    private static readonly object lockObj = new();
    public static int PlayersPerMatch = 2;

    public static void JoinQueue(Session session)
    {
        List<Session> toNotify;
        int currentCount;
        bool matchReady = false;

        if (session?.PlayerData == null)
        {
            Console.WriteLine("Hata: Session veya PlayerData null!");
            return;
        }

        lock (lockObj)
        {
            if (waitingQueue.Contains(session)) return;

            waitingQueue.Add(session);
            Console.WriteLine($"{session.PlayerData.Username} matchmaking'e katildi. Toplam: {waitingQueue.Count}");

            toNotify = waitingQueue.ToList();
            currentCount = waitingQueue.Count;

            if (waitingQueue.Count >= PlayersPerMatch)
            {
                matchReady = true;
            }
        }

        if (matchReady)
        {
            MatchFound();
            return;
        }

        var packet = new MatchMakingAddPlayerPacket
        {
            PlayersPerMatch = PlayersPerMatch,
            CurrentPlayers = currentCount,
        };
        foreach (var player in toNotify)
        {
            player.Send(packet);
        }
    }

    private static void MatchFound()
    {
        List<Session> players;
        List<string> usernames;
        lock (lockObj)
        {
            players = waitingQueue.Take(PlayersPerMatch).ToList();
            usernames = players.Select(p => p.PlayerData?.Username ?? "Unknown").ToList();
            waitingQueue.RemoveRange(0, PlayersPerMatch);
        }

        Console.WriteLine($"[MatchMaking] Match found! {string.Join(", ", usernames)}");

        int battlePort = (Config.Instance?.Port ?? 5000) + 100;

        var battleServer = GameServer.BattleServer;
        if (battleServer != null)
            battleServer.StartMatch(PlayersPerMatch);

        var packet = new MatchFoundPacket
        {
            Tick = TickManager.instance.Get_Tick(),
            BattlePort = battlePort,
            PlayerCount = PlayersPerMatch,
        };

        foreach (var session in players)
        {
            if (session?.PlayerData == null) continue;

            session.ChangeState(PlayerState.Battle);
            session.Send(packet);
            Console.WriteLine($"{session.PlayerData.Username} maça eklendi (port {battlePort}).");
        }
    }

    public static void RemoveQueue(Session session)
    {
        List<Session> toNotify = new();
        int currentCount = 0;

        if (session?.PlayerData == null)
        {
            Console.WriteLine("Hata: Session veya PlayerData null!");
            return;
        }

        lock (lockObj)
        {
            if (waitingQueue.Contains(session))
            {
                waitingQueue.Remove(session);
                Console.WriteLine($"{session.PlayerData.Username} kuyruktan kaldırıldı!");
                toNotify = waitingQueue.ToList();
                currentCount = waitingQueue.Count;
            }
            else
            {
                Console.WriteLine($"{session.PlayerData.Username} kuyrukta değilki?!");
                return;
            }
        }

        var packet = new MatchMakingAddPlayerPacket
        {
            PlayersPerMatch = PlayersPerMatch,
            CurrentPlayers = currentCount,
        };
        foreach (var player in toNotify)
        {
            player.Send(packet);
        }
    }
}
