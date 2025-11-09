using System.Numerics;

public static class MatchMaking
{
    public static readonly List<Session> waitingQueue = new();
    private static readonly object lockObj = new();
    private const int PlayersPerMatch = 2;

    public static void JoinQueue(Session session)
    {
        lock (lockObj)
        {
            if (waitingQueue.Contains(session))
                return;

            waitingQueue.Add(session);
            Console.WriteLine($"{session.PlayerData?.Username} matchmaking'e katıldı. Toplam: {waitingQueue.Count}");
            foreach(var player in waitingQueue)
            {
                ByteBuffer buffer = new ByteBuffer();
                buffer.WriteInt((int)MessageType.MatchMakingAddPlayer);
                buffer.WriteShort(1);
                byte[] bytes = buffer.ToArray();
                buffer.Dispose();
                player.Send(bytes);
            }

            if (waitingQueue.Count >= PlayersPerMatch)
            {
                
                StartMatch();
            }
        }
    }


    private static void StartMatch()
    {

        List<Session> players = waitingQueue.Take(PlayersPerMatch).ToList();
        waitingQueue.RemoveRange(0, PlayersPerMatch);

        int arenaId = ArenaManager.CreateArena();
        Console.WriteLine($"Match başladı! Arena ID: {arenaId}");
        Vector2[] spawnPoints =
    {
        new Vector2(-3, 0),
        new Vector2(3, 0),
        new Vector2(0, 3),
        new Vector2(0, -3)
    };

        int spawnIndex = 0;
        foreach (var session in players)
        {
             var spawn = spawnPoints[spawnIndex % spawnPoints.Length];
            spawnIndex++;
            var player = new Player
            {
                AccountId = session?.PlayerData?.AccountId,
                Username = session?.PlayerData?.Username ?? "No Name",
                Health = 100,
                Position = new Vector2(spawn.X, spawn.Y),
                ArenaId = arenaId,
                session = session

            };
            session.PlayerData = player;

           ArenaManager.AddPlayer(arenaId, player);

        }

          var allplayers = ArenaManager.GetPlayers(arenaId);
            //client to send is matchfound packets:
           foreach (var session in players)
{
    ByteBuffer buffer = new ByteBuffer();
    buffer.WriteInt((int)MessageType.MatchFound);
    buffer.WriteInt(allplayers.Count);

    foreach (var p in allplayers)
    {
        buffer.WriteString(p.AccountId);
        buffer.WriteString(p.Username);
        buffer.WriteFloat(p.Position.X);
        buffer.WriteFloat(p.Position.Y);
    }

    session.Send(buffer.ToArray());
    buffer.Dispose();
}
    }
    public static void RemoveQueue(Session session)
    {
        if (waitingQueue.Contains(session))
        {
            waitingQueue.Remove(session);
            Console.WriteLine($"{session?.PlayerData?.Username} kuyruktan kaldırıldı!");
            foreach(var player in waitingQueue)
            {
                ByteBuffer buffer = new ByteBuffer();
                buffer.WriteInt((int)MessageType.MatchMakingAddPlayer);
                buffer.WriteShort(PlayersPerMatch);
                buffer.WriteShort(1);
                byte[] bytes = buffer.ToArray();
                buffer.Dispose();
                player.Send(bytes);
            }
            
        }
        else  Console.WriteLine($"{session?.PlayerData?.Username} kuyrukta değilki?!");

    }
    

    
}