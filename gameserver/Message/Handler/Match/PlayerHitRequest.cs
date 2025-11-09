

public static class PlayerHitRequest
{
        public static void Handle(Session session, byte[] message)
    {
        Console.WriteLine("playerhit in run");
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        int type = read.ReadInt();

        string targetid = read.ReadString();
        float damage = read.ReadFloat();
        read.Dispose();

       
        var players = ArenaManager.GetPlayers(session.PlayerData.ArenaId);
        var player = players.FirstOrDefault(p => p.AccountId == targetid);
        
        ByteBuffer buffer = new ByteBuffer();
        // todo control
        player.Health -= (int)damage;
        if (player.Health <= 0)
        {
            buffer.WriteInt((int)MessageType.PlayerDead);
            buffer.WriteString(player.AccountId);
        }
        else
        {
            buffer.WriteInt((int)MessageType.PlayerHealthUpdate);
            buffer.WriteString(player.AccountId);
            buffer.WriteInt(player.Health);
        }
        byte[] data = buffer.ToArray();
        buffer.Dispose();
                    
         foreach (var p in players)
        {
            p.session.Send(data);
        }
              
           
            
       

    }

  
}