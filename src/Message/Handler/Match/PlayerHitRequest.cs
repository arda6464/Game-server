using System.Numerics;

[PacketHandler(MessageType.HitRequest)]
public static class PlayerHitRequest
{
    public static void Handle(Session session, byte[] message)
    {
        if (session.PlayerData == null) return;
        
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        
        var request = new HitRequestPacket();
        request.Deserialize(read);
        
        int targetid = request.TargetID;
        int bulletid = request.BulletId;
        read.Dispose();

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

        // Kendine vurma kontrolü (Internal ID üzerinden)
        if (targetid == session.ID)
        {
            Console.WriteLine("🚫 Kendine vurma engellendi");
            return;
        }

        targetplayer.Health -= bullet.Damage;
        
        Console.WriteLine($"💥 Hasar: {targetplayer.ID} -> Kalan can: {targetplayer.Health}");

        // ✅ Mermiyi sil
        battle.RemoveBullet(bulletid);

        // ✅ ÖLÜM KONTROLÜ
        if (targetplayer.Health <= 0)
        {
            battle.OnPlayerDied(targetplayer.ID, session.ID);
        }
        else
        {
            SendHealthUpdate(targetplayer.ID, targetplayer.Health, targetplayer.session);
        }
    }

    private static void SendHealthUpdate(int playerId, int health, Session? targetSession)
    {
        if (targetSession == null) return;

        var packet = new PlayerHealthUpdatePacket
        {
            PlayerID = playerId,
            Health = health
        };
        
        targetSession.Send(packet);
        
        Console.WriteLine($"❤️ Can güncellemesi: {playerId} -> {health}");
    }
}
