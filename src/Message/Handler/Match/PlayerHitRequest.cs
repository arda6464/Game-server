using System.Numerics;

[PacketHandler(MessageType.HitRequest)]
public static class PlayerHitRequest
{
    public static void Handle(Session session, byte[] message)
    {
        Console.WriteLine("🎯 PlayerHitRequest çalışıyor...");
        
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        int _ = read.ReadShort();
        
        var request = new HitRequestPacket();
        request.Deserialize(read);
        
        string targetid = request.TargetId;
        int bulletid = request.BulletId;
        read.Dispose();

        Console.WriteLine($"🔫 Hasar paketi: Hedef={targetid}, Mermi={bulletid}, Gönderen={session.AccountId}");

        // Kendine vurma kontrolü
       

        Battle battle = ArenaManager.GetBattle(session.PlayerData.BattleId);
        if (battle == null)
        {
            Console.WriteLine("❌ Battle bulunamadı");
            return;
        }

        Bullet bullet = battle.GetBullet(bulletid);
        var targetplayer = battle.GetPlayer(targetid);

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
        battle.RemoveBullet(bulletid);

        // ✅ ÖLÜM KONTROLÜ
        if (targetplayer.Health <= 0)
        {
            battle.OnPlayerDied(targetplayer.AccountId, session.AccountId);
        }
        else
        {
            SendHealthUpdate(targetplayer.AccountId, targetplayer.Health, targetplayer.session);
        }
    }


    private static void SendHealthUpdate(string playerId, int health, Session targetSession)
    {
        var packet = new PlayerHealthUpdatePacket
        {
            PlayerId = playerId,
            Health = health
        };
        
        targetSession.Send(packet);
        
        Console.WriteLine($"❤️ Can güncellemesi: {playerId} -> {health}");
    }
}