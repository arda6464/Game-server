using DietPhysics;
using Network;

public static class UdpGameHandler
{


    public static void HandleInput(Session session, ByteBuffer buffer, int seqNo)
    {
        var packet = new PlayerInputPacket();
        packet.SequenceNumber = seqNo;
        packet.Deserialize(buffer);

        if (session.PlayerData == null || !session.PlayerData.IsAlive)
            return;

        Battle battle = ArenaManager.GetBattle(session.PlayerData.BattleId);
        if (battle == null)
            return;

        float now = TickManager.GetCurrentTime();

        if (packet.AimByte == 0)
        {
            session.PlayerData.AimStarted = 0f;
            session.PlayerData.AimDirection = Vec3.zero;
         //   Console.WriteLine("[BATTLE] AİM BIRAKILDI");
        }
        else
        {
            float normalized = (packet.AimByte - 1) / 254f;
            float angle = normalized * MathF.PI * 2f;
            Vec3 aimDirection = new Vec3(MathF.Cos(angle), 0f, MathF.Sin(angle));

            if (session.PlayerData.AimDirection == Vec3.zero)
                session.PlayerData.AimStarted = now;

            session.PlayerData.AimDirection = aimDirection.normalized;
         //   Console.WriteLine("[BATTLE] AİM TUTULUYOR");
        }

        session.PlayerData.InputQueue.Enqueue(new PendingInput
        {
            Tick = packet.Tick,
            Direction = new Vec3(packet.InputX, 0, packet.InputY)
        });
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
            session.SendUnreliableUDP(response.GetBufferSegment());
        }
    }
    public static void  HandlePickUpRequest(Session session, ByteBuffer buffer, int seqNo)
    {
       int lootId = buffer.ReadVarInt();

        if (session.PlayerData == null) return;

        Battle battle = ArenaManager.GetBattle(session.PlayerData.BattleId);
        if (battle == null) return;

         battle.PickupStart(session.PlayerData.ID, lootId);
        
    }
    public static void HandleChangeSlotRequest(Session session, ByteBuffer buffer, int seqNo)
    {
        int toSlot = buffer.ReadVarInt();

        if (session.PlayerData == null) return;

        Battle battle = ArenaManager.GetBattle(session.PlayerData.BattleId);
        if (battle == null) return;

        battle.ChangePlayerSlot(session.PlayerData.ID,  toSlot);
    }
}
