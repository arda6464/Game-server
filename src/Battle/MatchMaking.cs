using System.Numerics;
using Logic;

public static class MatchMaking
{
    public static readonly List<Session> waitingQueue = new();
    private static readonly object lockObj = new();
    public static int PlayersPerMatch = 2;

    public static void JoinQueue(Session session)
    {
        lock (lockObj)
        {
            if (waitingQueue.Contains(session)) return;
                

            waitingQueue.Add(session);
            Console.WriteLine($"{session.PlayerData?.Username} matchmaking'e katıldı. Toplam: {waitingQueue.Count}");
             MatchMakingAddPlayerPacket packet = new MatchMakingAddPlayerPacket
                {
                    PlayersPerMatch = PlayersPerMatch,
                    CurrentPlayers = waitingQueue.Count,
                };
            foreach(var player in waitingQueue)
            {
                 player.Send(packet);
            }

            if (waitingQueue.Count >= PlayersPerMatch)
            {
                
                MatchFound();
            }
        }
    }


    private static void MatchFound()
    {

        List<Session> players = waitingQueue.Take(PlayersPerMatch).ToList();
        waitingQueue.RemoveRange(0, PlayersPerMatch);

        int battleId = ArenaManager.CreateBattle();
          Battle battle = ArenaManager.GetBattle(battleId);
        Console.WriteLine($"Match başladı! Battle ID: {battleId}");
       
   

      
        MapData map = MapManager.GetRandomMap();
        battle.Map = map;
        
      //  int index = 0;
        foreach (var session in players)
        {
            int spawnIndex = 1;
            Vector3 spawnPoint = map.SpawnPoints[spawnIndex];
            
            var player = new Player
            {
                AccountId = session?.PlayerData?.AccountId,
                Username = session?.PlayerData?.Username ?? "No Name",
                Health = 100,
                session = session,
                Position = new Vector3(10,2,10),
                SpawnIndex = spawnIndex
            };
            session.PlayerData = player;
            session.ChangeState(PlayerState.Battle);

            battle.AddPlayer(player);
            Console.WriteLine($"Oyuncu {player.Username} ({player.AccountId});" +
                $" Battle {battleId}'ye eklendi. Harita: {map.Name} SpawnIndex: {spawnIndex}");
           
        }
        
        battle.Start();

        var allplayers = battle.GetPlayers();
        
        // Create the packet once
        var packet = new MatchFoundPacket();
        foreach (var p in allplayers)
        {
            packet.Players.Add(p);
        }

        // Send the same packet to all clients in the match
        foreach (var session in players)
        {
            session.Send(packet);
        }




    }
    public static void RemoveQueue(Session session)
    {
        lock (lockObj)
        {
            if (waitingQueue.Contains(session))
            {
                waitingQueue.Remove(session);
                Console.WriteLine($"{session?.PlayerData?.Username} kuyruktan kaldırıldı!");
                MatchMakingAddPlayerPacket packet = new MatchMakingAddPlayerPacket
                {
                    PlayersPerMatch = PlayersPerMatch,
                    CurrentPlayers = waitingQueue.Count,
                };
                foreach(var player in waitingQueue)
                {
                  
                    player.Send(packet);
                }
            }
            else  Console.WriteLine($"{session?.PlayerData?.Username} kuyrukta değilki?!");
        }
    }
    

    
}