using Logic;
using DietPhysics;

partial class Battle
{
    private void SendHitConfirm(Player myPlayer, int TargetId, int Damage, bool IsHitShield)
    {
        myPlayer.OnHit += Damage;
        if (myPlayer?.session == null)
        {
            Logger.battlelog($"[BATTLE {BattleId}] Hit confirm skipped: shooter session missing target={TargetId} damage={Damage} shield={IsHitShield}");
            return;
        }

        ByteBuffer buffer = ByteBufferPool.Get();

        var packet = new HitConfirmPacket
        {
            TargetID = TargetId,
            Damage = Damage,
            Shield = IsHitShield
        };
        Logger.battlelog($"[BATTLE {BattleId}] Sending HitConfirm: shooter={myPlayer.Username} target={TargetId} damage={Damage} shield={IsHitShield}");
        packet.Serialize(buffer);
        buffer.Dispose();
        myPlayer.session.SendReliableUDP(packet);
        Logger.battlelog($"[BATTLE {BattleId}] HitConfirm sent: shooter={myPlayer.Username} target={TargetId} damage={Damage} shield={IsHitShield}");
    }

    private void SendUpdateHealth(Player myPlayer, int health, int Shield)
    {
        if (myPlayer?.session == null)
        {
            Logger.battlelog($"[BATTLE {BattleId}] Health update skipped: player session missing player={myPlayer?.Username} health={health} shield={Shield}");
            return;
        }

        ByteBuffer buffer = ByteBufferPool.Get();

        var packet = new PlayerHealthUpdatePacket
        {
            Health = health,
            Shield = Shield
        };
        packet.Serialize(buffer);
        buffer.Dispose();
        myPlayer.session.SendReliableUDP(packet);
    }

    private void SendMatchResult(Player player, bool isWin, int placement, int playerCount, int rewardCoins)
    {
        if (player?.session == null)
        {
            Logger.battlelog($"[BATTLE {BattleId}] Match result skipped: player session missing player={player?.Username}");
            return;
        }

        int trophiesDelta = TrophyCalculator.CalculateTrophyDelta(playerCount, placement);
        int rewardXp = isWin ? 80 : 45;
        rewardXp += Math.Min(25, (player.Kill * 10) + (player.OnHit / 80));

        rewardCoins += Math.Min(20, (player.Kill * 15) + (player.OnHit / 100));

        var logic = player.session.Logic;
        var account = player.session.Account;
        if (logic != null)
        {
            logic.AddBattleRewards(trophiesDelta, rewardCoins, rewardXp, isWin, placement, playerCount);
        }

        int currentLevel = account?.Level ?? 1;
        int currentExperience = account?.Experience ?? 0;
        int currentTrophies = account?.Trophy ?? 0;

        var packet = new MatchResultPacket
        {
            Placement = placement,
            Kills = player.Kill,
            DamageDealt = player.OnDamage,
            HitDealt = player.OnHit,
            CurrentTrophies = currentTrophies,
            TrophiesDelta = trophiesDelta,
            RewardXp = rewardXp,
            Level = currentLevel,
            Experience = currentExperience,
            ExperienceToNextLevel = ProgressionManager.GetRequiredExperienceForLevel(currentLevel),
            ElapsedTime = GetElapsedTime()
        };

        Logger.battlelog($"[BATTLE {BattleId}] Match result sent: player={player.Username} win={isWin} placement={placement} playerCount={playerCount} kills={player.Kill} damage={player.OnDamage} trophies={trophiesDelta} coins={rewardCoins}");
        player.session.Send(packet);
    }

    public void BroadcastSnapshot()
    {
        lock (_lock)
        {
            uint serverTick = TickManager.instance.Get_Tick();
            float currentTime = GetCurrentTime();

            foreach (var pSource in Players)
            {
                pSource.LastSentPosition = pSource.Position;
                pSource.LastSentRotation = pSource.Rotation;

                foreach (var pTarget in Players)
                {
                    if (pTarget.session?.UdpEndPoint == null) continue;

                    bool isVisible = true;
                    if (pSource.ID != pTarget.ID)
                    {
                        if (pSource.CurrentBushId != null)
                        {
                            bool sameBush = pSource.CurrentBushId == pTarget.CurrentBushId;
                            bool recentlyShot = (currentTime - pSource.LastShotTime) < 1.5f;
                            isVisible = sameBush || recentlyShot;
                        }
                    }

                    var packet = new PlayerMovePacket
                    {
                        ServerTick = serverTick,
                        LastProcessedInputTick = pSource.LastProcessedTick,
                        ID = pSource.ID,
                        X = pSource.Position.x,
                        Y = pSource.Position.y,
                        Z = pSource.Position.z,
                        IsVisible = isVisible
                    };

                    using (ByteBuffer payloadBuffer = ByteBufferPool.Get())
                    {
                        packet.Serialize(payloadBuffer);
                        pTarget.session.SendUnreliableUDP_Payload(payloadBuffer.GetBufferSegment());
                    }
                }
            }
        }
    }

    public void SendToAllPlayer(IPacket packet, bool Reliable)
    {
        foreach (var player in Players)
        {
            if (player.session == null) continue;

            if (Reliable) player.session.SendReliableUDP(packet);
            else player.session.SendUnreliableUDP(packet);
        }
    }

    private void BroadcastLootDeletion(int lootId)
    {
        lock (_lock)
        {
            Loots.RemoveAll(l => l.LootId == lootId);
        }
        var packet = new LootDeletedPacket
        {
            LootId = lootId
        };
        foreach (var player in GetPlayers())
            player.session?.SendReliableUDP(packet);
    }

    public static byte DirectionToAimByte(Vec3 dir)
    {
        if (dir == Vec3.zero)
            return 0;

        float angle = MathF.Atan2(dir.z, dir.x);
        if (angle < 0f)
            angle += MathF.PI * 2f;

        float normalized = angle / (MathF.PI * 2f);

        return (byte)Math.Clamp((int)MathF.Round(normalized * 254f) + 1, 1, 255);
    }

    private static string FormatVec3(Vec3 value)
    {
        return $"({value.x:0.##}, {value.y:0.##}, {value.z:0.##})";
    }

    private string GetElapsedTime()
    {
        DateTime Now = DateTime.Now;
        TimeSpan Finish = Now - _startTime;

        int minutes = (int)Finish.TotalMinutes;
        int seconds = Finish.Seconds;

        return $"{minutes:D2}:{seconds:D2}";
    }

    private float GetCurrentTime()
    {
        return TickManager.GetCurrentTime();
    }
}
