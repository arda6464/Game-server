using DietPhysics;
using Network;

public static class UdpGameHandler
{


    public static void HandleInput(Session session, ByteBuffer buffer, int seqNo)
    {
        var packet = new PlayerInputPacket();
        packet.SequenceNumber = seqNo;
        packet.Deserialize(buffer);

        if (session.PlayerData != null && session.PlayerData.IsAlive)
        {
            float moveX = packet.InputX;
            float moveZ = packet.InputY;

            session.PlayerData.InputQueue.Enqueue(new PendingInput
            {
                Tick = packet.Tick,
                Direction = new Vec3(moveX, 0, moveZ)
            });
        }
    }
    public static void HandleConnect(Session session)
    {
        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            int seqNo = session.GetNextReliableSequence();
            var packet = new UdpConnectionPacket
            {
                seqNo = seqNo,
            };
            packet.Serialize(buffer);
            session.SendReliableUDP(buffer.ToArray(), seqNo);
        }
    }


   


    public static void HandleShoot(Session session, ByteBuffer buffer, int seqNo)
    {
        var packet = new PlayerShootPacket();
        packet.SequenceNumber = seqNo;
        packet.Deserialize(buffer);

        // Shoot reliable olmalı veya hemen işlenmeli
        if (session.PlayerData == null) return;

        Battle battle = ArenaManager.GetBattle(session.PlayerData.BattleId);
        if (battle == null) return;

       /* Bullet bullet = new Bullet
        {
            BulletId = battle.GetNextBulletId(),
            Position = new Vector2(session.PlayerData.Position.X, session.PlayerData.Position.Z),
            Direction = Vector2.Normalize(new Vector2(packet.DirectionX, packet.DirectionY)),
            Speed = 10f,
            OwnerID = session.ID,
            startPos = new Vector2(session.PlayerData.Position.X, session.PlayerData.Position.Z),
            Damage = 50,
            menzil = 7f
        };
        battle.AddBullet(bullet);*/


        // Mermiyi diğer oyunculara bildir (Reliable UDP veya TCP)
        // Şimdilik UDP ile geri gönderelim
        var response = new PlayerShootPacket
        {
            SequenceNumber = packet.SequenceNumber,
            OwnerID = session.ID,
            DirectionX = packet.DirectionX,
            DirectionY = packet.DirectionY,
          //  BulletId = bullet.BulletId
        };

        // Arenadaki herkese gönder (Reliable UDP)
        foreach (var p in battle.GetPlayers())
        {
            if (p.session == null) continue;

            using (ByteBuffer sendBuffer = ByteBufferPool.Get())
            {
                response.SequenceNumber = p.session.GetNextReliableSequence();
                response.Serialize(sendBuffer);
                p.session.SendReliableUDP(sendBuffer.ToArray(), response.SequenceNumber);
            }
        }




    }
    public static void HandlePing(Session session, ByteBuffer buffer)
    {
        PingPacket pingPacket = new PingPacket();
        pingPacket.Deserialize(buffer);

        using (ByteBuffer response = ByteBufferPool.Get())
        {
            ushort seqNo = 0; // Unreliable, sıra numarası gönderilmeli (client header'ı okur)
            response.WriteVarInt((int)UdpPacketFlags.None);
            response.WriteVarInt(seqNo);         // seqNo (VarInt) — eksikti
            response.WriteVarInt((int)UdpMessageType.Pong);
            response.WriteFloat(pingPacket.ClientSentTime);
            session.SendUnreliableUDP(response.ToArray());
        }
    }
}
