using System.Numerics;

public static class UdpGameHandler
{
  

    public static void HandleInput(Session session, ByteBuffer buffer, ushort seqNo)
    {
        
        var packet = new PlayerInputPacket();
        packet.SequenceNumber = seqNo;
        packet.Deserialize(buffer);

        if (session.PlayerData != null && session.PlayerData.IsAlive)
        {
            // Client'tan gelen Joystick verisi (Genelde Y ekseni 2D UI'da dikey iken, 3D dünyada Z eksenini temsil eder)
            float moveX = packet.InputX;
            float moveZ = packet.InputY; 
            Console.WriteLine("Handle input geldi moveX: " + moveX + " moveZ: " + moveZ);
            
            // Eğer isMoving false ise input'u sıfırla
            if (!packet.IsMoving)
            {
                moveX = 0;
                moveZ = 0;
            }

            session.PlayerData.InputDirection = new Vector3(moveX, 0, moveZ);
            session.PlayerData.Rotation = packet.Rotation;
        }
    }


    public static void HandleMove(Session session, ByteBuffer buffer, ushort seqNo)
    {
        var packet = new PlayerMovePacket();
        packet.SequenceNumber = seqNo;
        packet.Deserialize(buffer);
        
        // HandleMove logic would go here. For now, it's just deserializing.
        // The original snippet had errors and seemed to copy from HandleShoot.
        // Assuming the intent was to add a new handler for PlayerMovePacket.
        if (session.PlayerData == null) return; // Example check, similar to HandleShoot
        // Further processing for PlayerMovePacket would be added here.
    }


    public static void HandleShoot(Session session, ByteBuffer buffer, ushort seqNo)
    {
        var packet = new PlayerShootPacket();
        packet.SequenceNumber = seqNo;
        packet.Deserialize(buffer);
        
        // Shoot reliable olmalı veya hemen işlenmeli
        if (session.PlayerData == null) return;

        Battle battle = ArenaManager.GetBattle(session.PlayerData.BattleId);
        if (battle == null) return;

        Bullet bullet = new Bullet
        {
            BulletId = battle.GetNextBulletId(),
            Position = new Vector2(session.PlayerData.Position.X, session.PlayerData.Position.Y),
            Direction = Vector2.Normalize(new Vector2(packet.DirectionX, packet.DirectionY)),
            Speed = 10f,
            OwnerId = session.AccountId,
            startPos = new Vector2(session.PlayerData.Position.X, session.PlayerData.Position.Y),
            Damage = 50,
            menzil = 7f
        };
        battle.AddBullet(bullet);


        // Mermiyi diğer oyunculara bildir (Reliable UDP veya TCP)
        // Şimdilik UDP ile geri gönderelim
        var response = new PlayerShootPacket
        {
           SequenceNumber = packet.SequenceNumber,
           OwnerId = session.AccountId,
           DirectionX = packet.DirectionX,
           DirectionY = packet.DirectionY,
           BulletId = bullet.BulletId
        };
        
        // Arenadaki herkese gönder (Reliable UDP)
        foreach(var p in battle.GetPlayers())
        {
            if (p.session == null) continue;

            using (ByteBuffer sendBuffer = new ByteBuffer())
            {
                response.SequenceNumber = p.session.GetNextSequence();
                // response.ConnectionToken set etmiyoruz (Token gönderilmemesi için 0 kalıyor)
                response.Serialize(sendBuffer);
                p.session.SendReliableUDP(sendBuffer.ToArray(), response.SequenceNumber);
            }

        }


    }
}
