using System.Numerics;

public static class PlayerHitRequest
{
    public static void Handle(Session session, byte[] message)
    {
        Console.WriteLine("🎯 PlayerHitRequest çalışıyor...");
        
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        int type = read.ReadInt();
        string targetid = read.ReadString();
        int bulletid = read.ReadInt();
        read.Dispose();

        Console.WriteLine($"🔫 Hasar paketi: Hedef={targetid}, Mermi={bulletid}, Gönderen={session.AccountId}");

        // Kendine vurma kontrolü
       

        Arena arena = ArenaManager.GetArena(session.PlayerData.ArenaId);
        if (arena == null)
        {
            Console.WriteLine("❌ Arena bulunamadı");
            return;
        }

        Bullet bullet = arena.GetBullet(bulletid);
        var targetplayer = arena.GetPlayer(targetid);

        if (targetplayer == null)
        {
            Console.WriteLine("❌ Hedef oyuncu bulunamadı");
            return; 
        }

        if (bullet == null)
        {
            Console.WriteLine($"❌ Mermi {bulletid} bulunamadı");
            return;
        }
        if (targetid == session.AccountId || bullet.OwnerId == targetid)
{
    Console.WriteLine("🚫 Kendine vurma engellendi");
    return;
}

        
    
        targetplayer.Health -= bullet.Damage;
        
        Console.WriteLine($"💥 Hasar: {targetplayer.AccountId} ->  Kalan can: {targetplayer.Health}");

        // ✅ Mermiyi sil
        arena.RemoveBullet(bulletid);

        // ✅ ÖLÜM KONTROLÜ
        if (targetplayer.Health <= 0)
        {
            SendDeathMessage(targetplayer.AccountId, session.AccountId, arena);
        }
        else
        {
            SendHealthUpdate(targetplayer.AccountId, targetplayer.Health, targetplayer.session);
        }
    }

    private static void SendDeathMessage(string deadPlayerId, string killerId, Arena arena)
    {
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.PlayerDead);
        buffer.WriteString(deadPlayerId);
        buffer.WriteString(killerId);
        
        byte[] deathData = buffer.ToArray();
        
        foreach (var player in arena.GetPlayers())
        {
            player.session.Send(deathData);
        }
        
        buffer.Dispose();
        Console.WriteLine($"💀 Ölüm haberi gönderildi: {deadPlayerId} -> {killerId}");
    }

    private static void SendHealthUpdate(string playerId, int health, Session targetSession)
    {
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.PlayerHealthUpdate);
        buffer.WriteString(playerId);
        buffer.WriteInt(health);
        
        byte[] healthData = buffer.ToArray();
        targetSession.Send(healthData);
        
        buffer.Dispose();
        Console.WriteLine($"❤️ Can güncellemesi: {playerId} -> {health}");
    }
}