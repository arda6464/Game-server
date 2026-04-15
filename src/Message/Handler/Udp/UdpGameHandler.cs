using System.Net.NetworkInformation;
using System.Numerics;
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
            // Client'tan gelen Joystick verisi
            float moveX = packet.InputX;
            float moveZ = packet.InputY;

            session.PlayerData.InputDirection = new Vector3(moveX, 0, moveZ);
            Console.WriteLine($"playerdata input save: name: {session?.Account?.Username} pozisyon:{session?.PlayerData.InputDirection}");
        }
    }
    public static void HandleConnect(Session session)
    {
        using (ByteBuffer buffer = new ByteBuffer())
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

        Bullet bullet = new Bullet
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
        battle.AddBullet(bullet);


        // Mermiyi diğer oyunculara bildir (Reliable UDP veya TCP)
        // Şimdilik UDP ile geri gönderelim
        var response = new PlayerShootPacket
        {
            SequenceNumber = packet.SequenceNumber,
            OwnerID = session.ID,
            DirectionX = packet.DirectionX,
            DirectionY = packet.DirectionY,
            BulletId = bullet.BulletId
        };

        // Arenadaki herkese gönder (Reliable UDP)
        foreach (var p in battle.GetPlayers())
        {
            if (p.session == null) continue;

            using (ByteBuffer sendBuffer = new ByteBuffer())
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

        using (ByteBuffer response = new ByteBuffer())
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
