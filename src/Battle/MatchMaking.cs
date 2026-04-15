using System.Numerics;
using Logic;

public static class MatchMaking
{
    public static readonly List<Session> waitingQueue = new();
    private static readonly object lockObj = new();
    public static int PlayersPerMatch = 2;

    public static void JoinQueue(Session session)
    {
        List<Session> toNotify;
        int currentCount;
        bool matchReady = false;

        lock (lockObj)
        {
            if (waitingQueue.Contains(session)) return;

            waitingQueue.Add(session);
            Console.WriteLine($"{session.PlayerData?.Username} matchmaking'e katildı. Toplam: {waitingQueue.Count}");

            toNotify = waitingQueue.ToList();
            currentCount = waitingQueue.Count;

            if (waitingQueue.Count >= PlayersPerMatch)
            {
                matchReady = true;
            }
        }

        // Lock dışında işlem yap — TCP bloklaması lockObj'i etkilemez
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

        List<Session> players = waitingQueue.Take(PlayersPerMatch).ToList();
        waitingQueue.RemoveRange(0, PlayersPerMatch);

        int battleId = ArenaManager.CreateBattle();
        Battle battle = ArenaManager.GetBattle(battleId);
        Console.WriteLine($"Match başladı! Battle ID: {battleId}");






        int index = 0;
        foreach (var session in players)
        {



            Player player = new Player
            {
                AccountId = session?.PlayerData?.AccountId,
                Username = session?.PlayerData?.Username ?? "No Name",
                Health = 100,
                session = session,
                Position = battle.SpawnPoints[index],
                StartPoint = battle.SpawnPoints[index],
            };
            session.PlayerData = player;
            session.ChangeState(PlayerState.Battle);
            battle.AddPlayer(player);
            Console.WriteLine($"{player.Username} adlı oyuncu şurada spawn olcak: {player.Position}");
            index++;
        }

        battle.Start();

        var allplayers = battle.GetPlayers();


        var packet = new MatchFoundPacket();
        packet.Tick = TickManager.instance.Get_Tick(); // Tüm oyunculara maçın başladığı anlık Tick'i bildiriyoruz
        ByteBuffer buffer = new ByteBuffer();
        packet.Players = allplayers;
        packet.Serialize(buffer);

        foreach (var session in players)
        {
            session.Send(packet);
        }
        buffer.Dispose();

    }
    public static void RemoveQueue(Session session)
    {
        List<Session> toNotify = new();
        int currentCount = 0;

        lock (lockObj)
        {
            if (waitingQueue.Contains(session))
            {
                waitingQueue.Remove(session);
                Console.WriteLine($"{session?.PlayerData?.Username} kuyruktan kaldırıldı!");
                toNotify = waitingQueue.ToList();
                currentCount = waitingQueue.Count;
            }
            else
            {
                Console.WriteLine($"{session?.PlayerData?.Username} kuyrukta değilki?!");
                return;
            }
        }

        // Lock dışında gönder
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