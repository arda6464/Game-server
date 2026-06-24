using Logic;
using DietPhysics;

public enum BattleState
{
    WaitingToStart,
    Active,
    Finished
}

public class Battle
{
    public int BattleId { get; set; }

    public BattleState State { get; private set; } = BattleState.WaitingToStart;

    public int BulletIdCounter = 0;
    public int LootIdCounter = 0;
    public List<Player> Players { get; set; } = new List<Player>();
    public List<Bullet> Bullets { get; set; } = new List<Bullet>();
    public List<LootItem> Loots { get; set; } = new List<LootItem>();
    public List<PickupData> Pickups { get; set; } = new List<PickupData>();
    public DateTime StartedAt { get; private set; } = DateTime.MinValue;


    private readonly object _lock = new object();
    private DateTime _startTime;
    private DietWorld World = new DietWorld();
    private const float PlayerRadius = 0.5f; // 
    private const float LootSpawnRadius = 0.35f;
    private const float MinLootDistanceFromPlayer = 1.0f;
    private const float MinLootDistanceFromLoot = 1.0f;
    private const int InitialWeaponSpawnCount = 10;
    private const int MaxPlayerHealth = 100;
    private const int MaxPlayerShield = 100;
    public List<Vec3> PlayerSpawnPoints = new List<Vec3>();

    public Battle()
    {
        try
        {
            MapManager.Load("MapData.json");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"[Battle] HATA: MapData.json yuklenemedi: {ex.Message}");
        }

        var map = MapManager.LoadedMap;

        // Duvarlar statik collider olarak ekleniyor.
        foreach (WallData wall in map.walls)
        {
            DietBox box = new DietBox(wall.pos, wall.center, wall.size, wall.rot, DietObjectType.Wall, 0);
            World.AddColliderStatic(box);
            Console.WriteLine($"[Harita] Duvar eklendi: pos={box.GetPosition()} size={box.Size}");
        }

        // Statik collider'larÄ± piÅŸir (spatial optimizasyon iÃ§in).
        World.Bake();

        PlayerSpawnPoints = map.spawnPoints;
        Console.WriteLine("----- Harita yüklendi -----");
    }

    public void Start()
    {
        lock (_lock)
        {
            if (State != BattleState.WaitingToStart) return;
            State = BattleState.Active;
            _startTime = DateTime.Now;
            StartedAt = _startTime;
            SpawnInitialLoots();
            Logger.battlelog($"[BATTLE {BattleId}] Battle started.");
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (State == BattleState.Finished) return;
            State = BattleState.Finished;
            Logger.battlelog($"[BATTLE {BattleId}] Battle stopped.");

            foreach (var player in Players)
            {
                if (player.session != null)
                {
                    player.BattleId = 0;
                    player.session.ChangeState(PlayerState.Lobby);
                    SessionManager.UnRegisterUdpSession(player.session.UdpEndPoint);
                }
            }

            ArenaManager.RemoveBattle(BattleId);
        }
    }

    public void Tick()
    {
        if (State != BattleState.Active) return;

        UpdateFireCooldown();
        UpdateAutoFire();
        UpdatePlayerPositions();
        UpdatePickups();
        UpdateBullets();
        BroadcastSnapshot();
        // removed checkmatchend --> just check died and removeplayer metod
    }

    public void AddBullet(Bullet bullet)
    {
        lock (_lock)
        {
            Bullets.Add(bullet);
            Logger.battlelog($"[BATTLE {BattleId}] Bullet added: id={bullet.BulletId} owner={bullet.OwnerID} pos={FormatVec3(bullet.Position)} dir={FormatVec3(bullet.Direction)} range={bullet.Range:0.##} speed={bullet.Speed:0.##}");
        }
    }
    public int GetNextBulletId()
    {
        return Interlocked.Increment(ref BulletIdCounter);
    }

    public void RemoveBullet(int bulletId)
    {
        lock (_lock)
        {
            Bullets.RemoveAll(b => b.BulletId == bulletId);
        }
    }

    public Bullet? GetBullet(int id)
    {
        lock (_lock)
        {
            return Bullets.FirstOrDefault(b => b.BulletId == id);
        }
    }

    private void UpdateBullets()
    {
        lock (_lock)
        {
            float currentTime = GetCurrentTime();
            foreach (var bullet in Bullets.ToList())
            {
                if (bullet.IsActive)
                {
                    float MoveDistance = bullet.Speed * TickManager.instance.DeltaTime;
                    DietSphere bulletSweep = new DietSphere(bullet.Position, Vec3.zero, 0.05f, DietObjectType.None, 0);
                    Logger.battlelog($"[BATTLE {BattleId}] Bullet tick: id={bullet.BulletId} pos={FormatVec3(bullet.Position)} move={MoveDistance:0.###} owner={bullet.OwnerID} active={bullet.IsActive}");
                    bool hit = World.SweepTest(
                        bulletSweep,
                        bullet.Direction.normalized,
                        MoveDistance,
                        8,
                        out Vec3 collidedPosition,
                        out DietObjectType hitType,
                        out int hitData);

                    if (hit)
                    {
                        bullet.Position = collidedPosition;
                        bullet.IsActive = false;
                        bullet.DeathTime = currentTime;
                        Logger.battlelog($"[BATTLE {BattleId}] Bullet hit: id={bullet.BulletId} hitType={hitType} hitData={hitData} pos={FormatVec3(collidedPosition)} owner={bullet.OwnerID}");

                        if (hitType == DietObjectType.Player)
                        {
                            var targetPlayer = GetPlayer(hitData);
                            if (targetPlayer != null && targetPlayer.ID != bullet.OwnerID && targetPlayer.IsAlive)
                            {
                                Logger.battlelog($"[BATTLE {BattleId}] Bullet damage candidate: bullet={bullet.BulletId} target={targetPlayer.Username}({targetPlayer.ID})");
                                OnDamage(targetPlayer, bullet);
                            }
                            else
                            {
                                string ignoreReason;
                                if (targetPlayer == null)
                                {
                                    ignoreReason = "target=null";
                                }
                                else if (targetPlayer.ID == bullet.OwnerID)
                                {
                                    ignoreReason = $"self-hit owner={bullet.OwnerID}";
                                }
                                else if (!targetPlayer.IsAlive)
                                {
                                    ignoreReason = $"target-dead health={targetPlayer.Health} shield={targetPlayer.Shield}";
                                }
                                else
                                {
                                    ignoreReason = $"unknown target={targetPlayer.Username}({targetPlayer.ID})";
                                }

                                Logger.battlelog($"[BATTLE {BattleId}] Bullet hit ignored: bullet={bullet.BulletId} owner={bullet.OwnerID} hitData={hitData} {ignoreReason}");
                            }
                        }
                        else
                        {
                            Logger.battlelog($"[BATTLE {BattleId}] Bullet collided with non-player object: bullet={bullet.BulletId} hitType={hitType} hitData={hitData}");
                        }
                    }
                    else
                    {
                        bullet.Position += bullet.Direction.normalized * MoveDistance;
                    }

                    float traveledDistance = Vec3.Distance(bullet.startPos, bullet.Position);
                    if (traveledDistance >= bullet.Range)
                    {
                        bullet.IsActive = false;
                        bullet.DeathTime = currentTime;
                        Logger.battlelog($"[BATTLE {BattleId}] Bullet expired by range: id={bullet.BulletId} traveled={traveledDistance:0.##} range={bullet.Range:0.##}");
                    }
                }

                if (!bullet.IsActive && (currentTime - bullet.DeathTime) > 3.0f)
                {
                    Bullets.Remove(bullet);
                    Logger.battlelog($"[BATTLE {BattleId}] Bullet removed: id={bullet.BulletId}");
                }
            }
        }
    }
    private void OnDamage(Player Targetplayer, Bullet bullet)
    {
        Logger.battlelog($"[BATTLE {BattleId}] Hit resolved: target={Targetplayer.Username}({Targetplayer.ID}) bullet={bullet.BulletId} owner={bullet.OwnerID} pos={FormatVec3(Targetplayer.Position)} damage={bullet.Damage} hp={Targetplayer.Health} shield={Targetplayer.Shield}");
        Targetplayer.OnDamage += bullet.Damage;
        Player attacker = GetPlayer(bullet.OwnerID);

        if (Targetplayer.Shield > 0)
        {
            int remainingShield = Targetplayer.Shield; // player shield
            int bulletDamage = bullet.Damage;

            if (bulletDamage < remainingShield)
            {
                Targetplayer.Shield -= bulletDamage;

            }
            else
            {
                int remainingDamage = bulletDamage - remainingShield;
                Targetplayer.Shield = 0;
                Targetplayer.Health -= remainingDamage;

            }
            SendHitConfirm(GetPlayer(bullet.OwnerID), Targetplayer.ID, bulletDamage, true);

            if (Targetplayer.Health <= 0)
            {
                OnPlayerDied(Targetplayer, bullet.OwnerID);
            }

            else SendUpdateHealth(Targetplayer, Targetplayer.Health, Targetplayer.Shield);
        }
        else
        {
            Targetplayer.Health -= bullet.Damage;

            SendHitConfirm(GetPlayer(bullet.OwnerID), Targetplayer.ID, bullet.Damage, false);
            if (Targetplayer.Health <= 0)
            {

                OnPlayerDied(Targetplayer, bullet.OwnerID);

            }
            else SendUpdateHealth(Targetplayer, Targetplayer.Health, Targetplayer.Shield);
        }
    }

    private void UpdateFireCooldown()
    {

        lock (_lock)
        {
            float now = GetCurrentTime();

            foreach (var player in Players)
            {
                if (player.ActiveGun == null)
                    continue;

                Gun gun = player.ActiveGun;

                if (!gun.IsReloading)
                    continue;

                if (now < gun.NextFireTime)
                    continue;

                gun.IsReloading = false;
            }

        }
    }


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
        int currentLevel = account?.Level ?? 1;
        int currentExperience = account?.Experience ?? 0;

        var packet = new MatchResultPacket
        {
            Placement = placement,
            Kills = player.Kill,
            DamageDealt = player.OnDamage,
            HitDealt = player.OnHit,
            CurrentTrophies = account.Trophy,
            TrophiesDelta = trophiesDelta,
            RewardXp = rewardXp,
            Level = currentLevel,
            Experience = currentExperience,
            ExperienceToNextLevel = ProgressionManager.GetRequiredExperienceForLevel(currentLevel),
            ElapsedTime = GetElapsedTime()

        };
        if (logic != null)
        {
            logic.AddBattleRewards(trophiesDelta, rewardCoins, rewardXp);
        }

        Logger.battlelog($"[BATTLE {BattleId}] Match result sent: player={player.Username} win={isWin} placement={placement} playerCount={playerCount} kills={player.Kill} damage={player.OnDamage} trophies={trophiesDelta} coins={rewardCoins}");
        player.session.Send(packet);
    }




    private void UpdateAutoFire()
    {
        float now = GetCurrentTime();

        // Snapshot al: AddPlayer/RemovePlayer başka thread'den çağrılabilir
        List<Player> snapshot;
        lock (_lock)
        {
            snapshot = Players.ToList();
        }

        foreach (var player in snapshot)
        {
            if (!player.IsAlive)
                continue;

            Gun gun = player.ActiveGun;
            if (gun == null)
                continue;

            Vec3 aimDir = player.AimDirection;
            if (aimDir == Vec3.zero)
                continue;

            if (now - player.AimStarted < 0.2f)
                continue;

            if (gun.CurrentAmmo <= 0)
                continue;

            if (now < gun.NextFireTime)
                continue;

            if (gun.IsReloading)
                continue;

            Logger.battlelog($"[BATTLE {BattleId}] AutoFire ready: player={player.Username} aimStarted={player.AimStarted:0.###} now={now:0.###} ammo={gun.CurrentAmmo} nextFire={gun.NextFireTime:0.###}");

            Fire(player);
        }
    }



    private void Fire(Player player)
    {
        if (player == null || player.ActiveGun == null) return;

        if (player.ActiveGun.IsReloading) return;

        Vec3 direction = player.AimDirection.normalized;
        if (direction == Vec3.zero)
        {
            Logger.battlelog($"[BATTLE {BattleId}] Fire skipped: zero aim direction player={player.Username}");
            return;
        }

        Vec3 spawnPos = player.Position + direction * (PlayerRadius + 0.1f);
        Logger.battlelog($"[BATTLE {BattleId}] Fire start: player={player.Username} pos={FormatVec3(player.Position)} spawn={FormatVec3(spawnPos)} dir={FormatVec3(direction)} ammo={player.ActiveGun.CurrentAmmo} reload={player.ActiveGun.IsReloading}");

        Bullet bullet = new Bullet
        {
            BulletId = GetNextBulletId(),
            startPos = spawnPos,
            Position = spawnPos,
            Direction = direction,
            Damage = player.ActiveGun.Damage,
            Speed = player.ActiveGun.ProjectileSpeed,
            Range = player.ActiveGun.Range,
            OwnerID = player.ID,
            Collider = new DietSphere(
                spawnPos,
                Vec3.zero,
                0.05f,
                DietObjectType.None,
                0
            )
        };

        AddBullet(bullet);
        Logger.battlelog($"[BATTLE {BattleId}] Fire bullet created: bullet={bullet.BulletId} owner={player.Username} ammoLeft={player.ActiveGun.CurrentAmmo - 1}");
        player.ActiveGun.CurrentAmmo--;

        var packet = new PlayerShootPacket
        {
            BulletId = bullet.BulletId,
            GunID = player.ActiveGun.WeaponId,
            StartPos = bullet.startPos,
            OwnerID = player.ID,
            aimbyte = DirectionToAimByte(bullet.Direction),
            RemaningAmmo = player.ActiveGun.CurrentAmmo
        };

        SendToAllPlayer(packet, true);

        player.ActiveGun.NextFireTime = GetCurrentTime() + player.ActiveGun.FireRate;
        player.ActiveGun.IsReloading = true;
        Logger.battlelog($"[BATTLE {BattleId}] Fire cooldown set: player={player.Username} nextFire={player.ActiveGun.NextFireTime:0.###} fireRate={player.ActiveGun.FireRate:0.###}");
    }
    private void UpdatePlayerPositions()
    {
        lock (_lock)
        {
            float deltaTime = TickManager.instance.DeltaTime;
            uint currentTick = TickManager.instance.Get_Tick();

            foreach (var player in Players)
            {
                // Depenetration: Oyuncu bir nesnenin iÃ§indeyse dÄ±ÅŸarÄ± it.
                if (player.Collider != null)
                {
                    if (World.ResolveOverlap(player.Collider, out Vec3 resolvedPos))
                    {
                        player.Position = resolvedPos;
                        player.Collider.Position = resolvedPos;
                    }
                }

                while (player.InputQueue.Count > 0)
                {
                    var input = player.InputQueue.Dequeue();
                    player.LastProcessedTick = input.Tick;

                    if (!player.IsAlive || input.Direction == Vec3.zero) continue;

                    Vec3 direction = input.Direction.normalized;
                    float distance = player.Speed * deltaTime;

                    if (player.Collider == null)
                    {
                        // Collider yoksa fizik kontrolÃ¼ yapma, doÄŸrudan hareket et.
                        player.Position += direction * distance;
                    }
                    else
                    {
                        ApplyMovementWithSliding(player, direction, distance);
                    }

                    if (player.session?.PlayerData != null)
                        player.session.PlayerData.Position = player.Position;
                }

                // Bu tick'teki pozisyonu kayÄ±t et.
                player.PositionHistory[currentTick] = player.Position;

                // 1 saniyeden eski pozisyon kayÄ±tlarÄ±nÄ± temizle.
                uint oldTick = currentTick > (uint)TickManager.instance.TickRate
                    ? currentTick - (uint)TickManager.instance.TickRate
                    : 0;
                player.PositionHistory.Remove(oldTick);
            }
        }
    }

    /// <summary>
    /// Ã–nce tam yÃ¶nde hareket dene, engel varsa X ve Z eksenlerinde ayrÄ± ayrÄ± kayma dene.
    /// </summary>
    private void ApplyMovementWithSliding(Player player, Vec3 direction, float distance)
    {
        int sweepIterations = Math.Max(5, (int)MathF.Ceiling(distance / Math.Max(PlayerRadius * 0.15f, 0.01f)));

        // Tam hareket mÃ¼mkÃ¼nse direkt ilerle.
        if (!World.SweepTest(player.Collider, direction, distance, sweepIterations, out _, out _, out _))
        {
            player.Position += direction * distance;
            player.Collider.Position = player.Position;
            return;
        }

        // Engel var â€” eksen bazlÄ± kayma dene.
        bool movedX = false;
        bool movedZ = false;

        if (MathF.Abs(direction.x) > 0.1f)
        {
            Vec3 xDir = new Vec3(direction.x, 0, 0).normalized;
            float xDist = distance * MathF.Abs(direction.x);
            if (!World.SweepTest(player.Collider, xDir, xDist, sweepIterations, out _, out _, out _))
            {
                player.Position += xDir * xDist;
                player.Collider.Position = player.Position;
                movedX = true;
            }
        }

        if (MathF.Abs(direction.z) > 0.1f)
        {
            Vec3 zDir = new Vec3(0, 0, direction.z).normalized;
            float zDist = distance * MathF.Abs(direction.z);
            if (!World.SweepTest(player.Collider, zDir, zDist, sweepIterations, out _, out _, out _))
            {
                player.Position += zDir * zDist;
                player.Collider.Position = player.Position;
                movedZ = true;
            }
        }

        if (!movedX && !movedZ)
            Console.WriteLine($"[Fizik] {player.Username} tamamen bloklandÄ±.");
    }

    /// <summary>
    /// Sunucuya gelen ham pozisyon paketini fizik doÄŸrulamasÄ±ndan geÃ§irir.
    /// GeÃ§ersizse oyuncuyu sÄ±nÄ±r noktasÄ±na Ã§eker.
    /// </summary>
    public void UpdatePlayerPosition(int id, Vec3 newPos)
    {
        lock (_lock)
        {
            var player = Players.FirstOrDefault(p => p.ID == id);
            if (player == null) return;

            if (player.Collider == null)
            {
                player.Position = newPos;
            }
            else
            {
                Vec3 delta = newPos - player.Position;
                float distance = delta.magnitude;
                int sweepIterations = Math.Max(5, (int)MathF.Ceiling(distance / Math.Max(PlayerRadius * 0.15f, 0.01f)));

                if (distance > 0.001f && World.SweepTest(player.Collider, delta.normalized, distance, sweepIterations, out Vec3 collidedPos, out _, out _))
                {
                    // Collision tespit edildi: collider yÃ¼zeyinin biraz gerisine al.
                    player.Position = collidedPos + delta.normalized * -0.01f;
                    Console.WriteLine($"[Fizik] {player.Username} paket ile duvara Ã§arptÄ±, sÄ±nÄ±rda tutuldu.");
                }
                else
                {
                    player.Position = newPos;
                }

                player.Collider.Position = player.Position;
            }

            if (player.session?.PlayerData != null)
                player.session.PlayerData.Position = player.Position;
        }
    }

    public void BroadcastSnapshot()
    {
        lock (_lock)
        {
            uint serverTick = TickManager.instance.Get_Tick();

            foreach (var pSource in Players)
            {
                pSource.LastSentPosition = pSource.Position;
                pSource.LastSentRotation = pSource.Rotation;

                var packet = new PlayerMovePacket
                {
                    ServerTick = serverTick,
                    LastProcessedInputTick = pSource.LastProcessedTick,
                    ID = pSource.ID,
                    X = pSource.Position.x,
                    Y = pSource.Position.y,
                    Z = pSource.Position.z,
                };

                using (ByteBuffer payloadBuffer = ByteBufferPool.Get())
                {
                    packet.Serialize(payloadBuffer);
                    var segment = payloadBuffer.GetBufferSegment();

                    foreach (var pTarget in Players)
                    {
                        if (pTarget.session?.UdpEndPoint != null)
                            pTarget.session.SendUnreliableUDP_Payload(segment);
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



    public Player? GetPlayer(int id)
    {
        lock (_lock)
        {
            return Players.FirstOrDefault(p => p.ID == id);
        }
    }

    public List<Player> GetPlayers()
    {
        lock (_lock)
        {
            return Players.ToList();
        }
    }

    public List<LootItem> GetLoots()
    {
        lock (_lock)
        {
            return Loots.ToList();
        }
    }

    public void AddPlayer(Player player)
    {
        lock (_lock)
        {
            player.BattleId = BattleId;

            int spawnIndex = Players.Count % PlayerSpawnPoints.Count;
            player.Position = PlayerSpawnPoints[spawnIndex];

            if (player.session?.PlayerData != null)
                player.session.PlayerData.Position = player.Position;

            Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} spawned at {FormatVec3(player.Position)}");

            player.Collider = new DietSphere(player.Position, Vec3.zero, PlayerRadius, DietObjectType.Player, player.ID);
            Logger.battlelog($"[BATTLE {BattleId}] Player collider added: player={player.Username} pos={FormatVec3(player.Position)} radius={PlayerRadius:0.##}");
            World.AddColliderDynamic(player.Collider);
            RefreshActiveGunFromSelectedSlot(player);

            Players.Add(player);
            Logger.battlelog($"[BATTLE {BattleId}] Player added: {player.Username} (Total: {Players.Count})");
        }
    }
    public void RemovePlayer(int id)
    {
        lock (_lock)
        {
            var player = Players.FirstOrDefault(p => p.ID == id);
            if (player?.Collider != null)
                World.RemoveColliderDynamic(player.Collider);

            if (player?.session != null)
            {
                SessionManager.UnRegisterUdpSession(player.session.UdpEndPoint);
            }

            Players.RemoveAll(p => p.ID == id);
            Logger.battlelog($"[BATTLE {BattleId}] Player removed: {id} (Remaining: {Players.Count})");
          
        }
    }

    public void OnPlayerDied(Player deadPlayer, int killerId)
    {
        if (deadPlayer == null) return;

        deadPlayer.IsAlive = false;
        deadPlayer.Health = 0;
        deadPlayer.ActiveGun = null;

        Player killerPlayer = GetPlayer(killerId);
        if (killerPlayer != null)
            killerPlayer.Kill++;


        var packet = new PlayerDeadPacket
        {
            DeadPlayerId = deadPlayer.ID,
            KillerId = killerId,
            GunID = killerPlayer?.ActiveGun?.WeaponId ?? 0
        };
        SendToAllPlayer(packet, true);

        int placement = Players.Count(p => p.IsAlive) + 1;
        int playerCount = Players.Count;
        SendMatchResult(deadPlayer, false, placement, playerCount, 0);

        /*------------------ Dead Player Loot System--------------- */

        var itemsToDrop = deadPlayer.InventorySlots.Where(s => s.DataId != 0).ToList();
        List<LootItem> loots = new List<LootItem>();


        for (int i = 0; i < itemsToDrop.Count; i++)
        {
            var slot = itemsToDrop[i];
            loots.Add(new LootItem
            {
                DataId = slot.DataId,
                Type = slot.Item
            });
            slot.DataId = 0;
            //slot.Item = null;
            slot.Gun = null;
        }
        while (loots.Count < 3) // min loot 3
        {
            LootItem randomLoot = RandomLoot(new[] { LootItemType.Ammo, LootItemType.Weapon });
            if (randomLoot != null) loots.Add(randomLoot);

        }


        float radius = 0.5f;
        int index = 0;
        foreach (var loot in loots)
        {

            float angle = index++ * (2.0f * MathF.PI / loots.Count);
            Vec3 spawnPos = new Vec3(
                deadPlayer.Position.x + radius * MathF.Cos(angle),
                deadPlayer.Position.y,
                deadPlayer.Position.z + radius * MathF.Sin(angle)
            );

            LootItem droppedLoot = ForceSpawnLoot(loot.Type, loot.DataId, spawnPos);


            var lootPacket = new LootSpawnedPacket
            {
                LootId = droppedLoot.LootId,
                Type = (int)droppedLoot.Type,
                DataId = droppedLoot.DataId,
                X = droppedLoot.Position.x,
                Y = droppedLoot.Position.y,
                Z = droppedLoot.Position.z
            };
            SendToAllPlayer(lootPacket, true);

        }

        deadPlayer.BattleId = 0;
       

         CheckMatchEnd();
    }

    private void CheckMatchEnd()
    {
        lock (_lock)
        {
            if (State != BattleState.Active) return;

            var alivePlayers = Players.Where(p => p.IsAlive).ToList();
            if (alivePlayers.Count == 1)
            {
                SendMatchResult(alivePlayers[0], true, 1, Players.Count, 100);
                Stop();
            }
            else if (alivePlayers.Count == 0)
            {
                Stop();
            }



        }
    }

    public int GetNextLootId()
    {
        return Interlocked.Increment(ref LootIdCounter);
    }

    public bool SpawnLoot(LootItemType type, int dataId, Vec3 position)
    {
        bool spawned = TrySpawnLoot(type, dataId, position);
        return spawned;

    }

    private static string FormatVec3(Vec3 value)
    {
        return $"({value.x:0.##}, {value.y:0.##}, {value.z:0.##})";
    }

    public bool TrySpawnWeapon(int weaponId, Vec3 position)
    {
        lock (_lock)
        {
            WeaponData? weapon = DataManager.GetWeapon(weaponId);
            if (weapon == null)
            {
                Logger.battlelog($"[BATTLE {BattleId}] Weapon spawn rejected: weapon not found ({weaponId})");
                return false;
            }

            if (!IsValidLootSpawnPosition(ref position, out string reason))
            {
                Logger.battlelog($"[BATTLE {BattleId}] Weapon spawn rejected: {weaponId} at {position}. Reason: {reason}");
                return false;
            }

            var loot = new LootItem
            {
                LootId = GetNextLootId(),
                DataId = weapon.Id,
                Type = LootItemType.Weapon,
                Position = position,
                SpawnTime = GetCurrentTime()
            };
            Loots.Add(loot);
            Logger.battlelog($"[BATTLE {BattleId}] Weapon spawned: {weapon.Name} ({weapon.Id}) at {FormatVec3(position)}");
            return true;
        }
    }

    public bool TrySpawnRandomWeapon(Vec3 position)
    {
        WeaponData[] weapons = DataManager.GetAllWeapons().ToArray();
        if (weapons.Length == 0)
        {
            Logger.battlelog($"[BATTLE {BattleId}] Weapon spawn rejected: no weapons loaded");
            return false;
        }

        int index = Random.Shared.Next(weapons.Length);
        return TrySpawnWeapon(weapons[index].Id, position);
    }
    private LootItem RandomLoot(LootItemType[] NotUseTypes = null)
    {
        NotUseTypes ??= Array.Empty<LootItemType>();
        Random random = new Random();

        // Default weights: Weapons (60%), Shields (20%), Health (20%)
        var weights = new Dictionary<LootItemType, int>
        {
            { LootItemType.Weapon, 65 },
            { LootItemType.Shield, 20 },
            { LootItemType.Health, 15 }
        };

        var availableItems = weights
            .Where(x => !NotUseTypes.Contains(x.Key))
            .ToList();

        if (availableItems.Count == 0)
        {
            throw new Exception("Kullanılabilir eşya kalmadı!");
        }

        int totalWeight = availableItems.Sum(x => x.Value);
        int roll = random.Next(totalWeight);

        LootItemType lootitem = availableItems[0].Key;
        int currentWeightSum = 0;
        foreach (var item in availableItems)
        {
            currentWeightSum += item.Value;
            if (roll < currentWeightSum)
            {
                lootitem = item.Key;
                break;
            }
        }

        LootItem loot = new LootItem();
        loot.Type = lootitem;

        switch (lootitem)
        {
            case LootItemType.Weapon:
                WeaponData[] weapons = DataManager.GetAllWeapons().ToArray();
                if (weapons.Length == 0)
                    throw new Exception("Kullanilabilir silah kalmadi!");

                loot.DataId = weapons[random.Next(weapons.Length)].Id;
                break;

            case LootItemType.Health:
            case LootItemType.Shield:
                LootData? lootData = GetRandomLootData(lootitem, random);
                if (lootData == null)
                    throw new Exception($"Kullanilabilir loot datasi yok: {lootitem}");

                loot.DataId = lootData.Id;
                break;
            default:
                throw new Exception($"Beklenmeyen eşya tipi: {lootitem}");
        }

        Logger.battlelog($"[BATTLE] RANDROM LOOT TYPE  {loot.Type} DATAİD {loot.DataId}");
        return loot;

    }

    private LootData? GetRandomLootData(LootItemType type, Random random)
    {
        LootData[] lootItems = DataManager.GetAllLootItems()
            .Where(l => l.Type == type && l.SpawnWeight > 0)
            .ToArray();

        if (lootItems.Length == 0)
            return null;

        int totalWeight = lootItems.Sum(l => l.SpawnWeight);
        int roll = random.Next(totalWeight);
        int currentWeight = 0;

        foreach (LootData lootItem in lootItems)
        {
            currentWeight += lootItem.SpawnWeight;
            if (roll < currentWeight)
                return lootItem;
        }

        return lootItems[^1];
    }

    public bool TrySpawnLoot(LootItemType type, int dataId, Vec3 position, bool ignoreDistanceChecks = false)
    {
        lock (_lock)
        {
            if (!IsValidLootData(type, dataId))
            {
                return false;
            }

            if (!ignoreDistanceChecks)
            {
                if (!IsValidLootSpawnPosition(ref position, out string reason))
                {
                    Logger.battlelog($"[BATTLE {BattleId}] Loot spawn rejected: {dataId} at {FormatVec3(position)}. Reason: {reason}");

                    return false;
                }
            }

            var loot = new LootItem
            {
                LootId = GetNextLootId(),
                DataId = dataId,
                Type = type,
                Position = position,
                SpawnTime = GetCurrentTime()
            };
            Loots.Add(loot);
            Logger.battlelog($"[BATTLE {BattleId}] Loot spawned: {dataId} at {FormatVec3(position)} (LootId: {loot.LootId})");

            return true;
        }
    }


    public LootItem ForceSpawnLoot(LootItemType type, int dataId, Vec3 position)
    {
        lock (_lock)
        {
            // Duvar içine düşme koruması
            DietSphere testSphere = new DietSphere(position, Vec3.zero, LootSpawnRadius, DietObjectType.None, 0);
            if (World.ResolveOverlap(testSphere, out Vec3 resolvedPos))
            {
                position = resolvedPos;
            }

            var loot = new LootItem
            {
                LootId = GetNextLootId(),
                DataId = dataId,
                Type = type,
                Position = position,
                SpawnTime = GetCurrentTime()
            };
            Loots.Add(loot);
            Logger.battlelog($"[BATTLE {BattleId}] Loot force-spawned: {dataId} at {FormatVec3(position)} (LootId: {loot.LootId})");
            return loot;
        }
    }

    private bool IsValidLootData(LootItemType type, int dataId)
    {
        if (type == LootItemType.Weapon)
        {
            if (DataManager.GetWeapon(dataId) != null)
                return true;

            Logger.battlelog($"[BATTLE {BattleId}] Loot spawn rejected: weapon not found ({dataId})");
            return false;
        }

        LootData? lootData = DataManager.GetLootItem(type, dataId);
        if (lootData == null)
        {
            Logger.battlelog($"[BATTLE {BattleId}] Loot spawn rejected: loot data not found ({dataId})");
            return false;
        }

        if (lootData.Type != type)
        {
            Logger.battlelog($"[BATTLE {BattleId}] Loot spawn rejected: data type mismatch data={dataId} expected={type} actual={lootData.Type}");
            return false;
        }

        return true;
    }

    private bool IsValidLootSpawnPosition(ref Vec3 position, out string reason)
    {
        DietSphere testSphere = new DietSphere(position, Vec3.zero, LootSpawnRadius, DietObjectType.None, 0);
        if (World.ResolveOverlap(testSphere, out Vec3 resolvedPos))
        {
            position = resolvedPos;
        }

        foreach (Player player in Players)
        {
            if (Vec3.Distance(position, player.Position) < MinLootDistanceFromPlayer)
            {
                reason = $"too close to player {player.ID}";
                return false;
            }
        }

        foreach (LootItem loot in Loots)
        {
            if (Vec3.Distance(position, loot.Position) < MinLootDistanceFromLoot)
            {
                reason = $"too close to loot {loot.LootId}";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    private void SpawnInitialLoots()
    {
        WeaponData[] weapons = DataManager.GetAllWeapons().ToArray();
        if (weapons.Length == 0)
        {
            Logger.battlelog($"[BATTLE {BattleId}] Initial weapon spawn skipped: no weapons loaded");
            return;
        }

        int targetSpawnCount = Math.Min(InitialWeaponSpawnCount, weapons.Length);
        int spawnedCount = 0;
        int attempts = 0;
        int maxAttempts = targetSpawnCount * 10;
        HashSet<string> usedPoints = new HashSet<string>();

        while (spawnedCount < targetSpawnCount && attempts < maxAttempts)
        {
            attempts++;

            LootItem loot = RandomLoot(new[] { LootItemType.Ammo });
            Vec3 position = MapManager.GetRandomLootPoint();
            string pointKey = $"{position.x:F1}:{position.y:F1}:{position.z:F1}";

            if (usedPoints.Contains(pointKey))
                continue;



            if (SpawnLoot(loot.Type, loot.DataId, position))
            {
                usedPoints.Add(pointKey);
                spawnedCount++;
            }
        }

        Logger.battlelog($"[BATTLE {BattleId}] Initial weapon spawn complete: {spawnedCount}/{targetSpawnCount}");
    }
    public void PickupStart(int playerId, int lootId)
    {
        var player = GetPlayer(playerId);
        var loot = Loots.FirstOrDefault(l => l.LootId == lootId);

        if (player == null || loot == null)
            return;

        if (Pickups.Any(p => p.PlayerId == playerId || p.LootId == lootId))
            return;

        if (!CanPickupLoot(player, loot))
        {
            var failPacket = new PickupResponsePacket
            {
                LootID = lootId,
                Success = false
            };
            player.session?.SendReliableUDP(failPacket);
            return;
        }

        float collectTime = GetPickupRequiredTime(loot);
        float now = GetCurrentTime();

        Pickups.Add(new PickupData
        {
            PlayerId = playerId,
            LootId = lootId,
            PickupTime = now,
            FinishTime = now + collectTime,
            RequiredTime = collectTime
        });
        var packet = new PickupResponsePacket
        {
            LootID = lootId,
            Success = true
        };
        player.session?.SendReliableUDP(packet);
        Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} started picking up loot {loot.LootId} ({collectTime:0.##}s)");
    }

    private float GetPickupRequiredTime(LootItem loot)
    {
        if (loot.Type == LootItemType.Weapon)
        {
            WeaponData? weapon = DataManager.GetWeapon(loot.DataId);
            return weapon?.CollectableTime ?? 1.0f;
        }

        LootData? lootData = DataManager.GetLootItem(loot.Type, loot.DataId);
        return lootData?.CollectableTime ?? 1.0f;
    }

    private bool CanPickupLoot(Player player, LootItem loot)
    {
        if (loot.Type == LootItemType.Health)
            return player.Health < MaxPlayerHealth;

        if (loot.Type == LootItemType.Shield)
            return player.Shield < MaxPlayerShield;

        return true;
    }

    public void UpdatePickups()
    {
        float currentTime = GetCurrentTime();
        foreach (var pickup in Pickups.ToArray())
        {
            var player = GetPlayer(pickup.PlayerId);
            var loot = Loots.FirstOrDefault(l => l.LootId == pickup.LootId);
            if (player == null || loot == null)
            {
                Pickups.Remove(pickup);
                continue;
            }

            if (Vec3.Distance(player.Position, loot.Position) > 1.0f)
            {
                Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} moved away from loot {loot.LootId}, pickup cancelled");
                Pickups.Remove(pickup);
                continue;
            }

            if (currentTime < pickup.FinishTime)
                continue;

            BroadcastLootDeletion(loot.LootId);

            Pickups.Remove(pickup);
            GiveItemToPlayer(player, loot);
            Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} picked up loot {loot.LootId} after {pickup.RequiredTime:0.##}s");
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
    private void GiveItemToPlayer(Player player, LootItem loot)
    {
        if (ApplyInstantLoot(player, loot))
            return;

        if (player.InventorySlots == null || player.InventorySlots.Count == 0)
        {
            Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} has no inventory slots.");
            return;
        }

        int targetSlot = GetFreeSlotIndex(player);
        if (targetSlot < 0)
            targetSlot = NormalizeSlotIndex(player.SelectedSlot, player.InventorySlots.Count);

        if (targetSlot < 0 || targetSlot >= player.InventorySlots.Count)
        {
            Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} has invalid target slot {targetSlot}.");
            return;
        }

        player.InventorySlots[targetSlot].DataId = loot.DataId;
        player.InventorySlots[targetSlot].Item = loot.Type;
        player.InventorySlots[targetSlot].Gun = null;

        if (loot.Type == LootItemType.Weapon)
        {
            WeaponData? weaponData = DataManager.GetWeapon(loot.DataId);
            if (weaponData != null)
            {
                Gun gun = Gun.FromWeaponData(weaponData);
                player.InventorySlots[targetSlot].Gun = gun;
                player.ActiveGun = gun;
            }
        }

        player.SelectedSlot = targetSlot;
        RefreshActiveGunFromSelectedSlot(player);

        var packet = new PlayerGivedItemPacket
        {
            playerId = player.ID,
            SlotId = player.SelectedSlot,
            ItemType = (int)loot.Type,
            DataId = loot.DataId
        };
        if (loot.Type == LootItemType.Weapon)
            foreach (var p in GetPlayers())
                p.session?.SendReliableUDP(packet);
        else
            player.session?.SendReliableUDP(packet);

        Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} received item {loot.DataId} in slot {player.SelectedSlot}");

    }

    private bool ApplyInstantLoot(Player player, LootItem loot)
    {
        LootData? lootData = DataManager.GetLootItem(loot.Type, loot.DataId);
        if (lootData == null || lootData.Type != loot.Type)
            return false;

        if (loot.Type == LootItemType.Health)
        {
            int oldHealth = player.Health;
            player.Health = Math.Min(MaxPlayerHealth, player.Health + lootData.Value);
            SendUpdateHealth(player, player.Health, player.Shield);
            Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} applied health loot {lootData.Name} ({oldHealth}->{player.Health})");
            return true;
        }

        if (loot.Type == LootItemType.Shield)
        {
            int oldShield = player.Shield;
            player.Shield = Math.Min(MaxPlayerShield, player.Shield + lootData.Value);
            SendUpdateHealth(player, player.Health, player.Shield);
            Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} applied shield loot {lootData.Name} ({oldShield}->{player.Shield})");
            return true;
        }

        return false;
    }

    public void ChangePlayerSlot(int playerId, int toSlot)
    {
        var player = GetPlayer(playerId);
        if (player == null) return;

        if (toSlot < 0 || toSlot >= player.InventorySlots.Count || player.SelectedSlot == toSlot)
            return;


        player.SelectedSlot = toSlot;
        RefreshActiveGunFromSelectedSlot(player);

        var packet = new ChangedSlotPacket
        {
            PlayerId = playerId,
            ToSlot = toSlot,
            Itemtype = player.InventorySlots[toSlot].Item,
            DataId = player.InventorySlots[toSlot].DataId
        };
        foreach (var p in GetPlayers())
            p.session?.SendReliableUDP(packet);

        Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username}  with slot {toSlot}");
    }

    private int GetFreeSlotIndex(Player player)
    {
        if (player.InventorySlots == null)
            return -1;

        for (int i = 0; i < player.InventorySlots.Count; i++)
        {
            if (player.InventorySlots[i].DataId == 0)
                return i;
        }

        return -1;
    }

    private int NormalizeSlotIndex(int slotIndex, int slotCount)
    {
        if (slotCount <= 0)
            return -1;

        if (slotIndex < 0)
            return 0;

        if (slotIndex >= slotCount)
            return slotCount - 1;

        return slotIndex;
    }

    private void RefreshActiveGunFromSelectedSlot(Player player)
    {
        if (player.InventorySlots == null || player.InventorySlots.Count == 0)
        {
            ClearActiveGun(player);
            return;
        }

        int slotIndex = NormalizeSlotIndex(player.SelectedSlot, player.InventorySlots.Count);
        if (slotIndex < 0)
        {
            ClearActiveGun(player);
            return;
        }

        var slot = player.InventorySlots[slotIndex];
        if (slot.Item != LootItemType.Weapon || slot.DataId == 0)
        {
            ClearActiveGun(player);
            return;
        }

        WeaponData? weapon = DataManager.GetWeapon(slot.DataId);
        if (weapon == null)
        {
            ClearActiveGun(player);
            return;
        }

        if (slot.Gun == null || slot.Gun.WeaponId != weapon.Id)
        {
            slot.Gun = Gun.FromWeaponData(weapon);
            player.InventorySlots[slotIndex].Gun = slot.Gun;
        }

        player.ActiveGun = slot.Gun;
    }

    private static void ClearActiveGun(Player player)
    {
        player.ActiveGun = null;
    }

    public static byte DirectionToAimByte(Vec3 dir)
    {
        if (dir == Vec3.zero)
            return 0; // aim yok

        float angle = MathF.Atan2(dir.z, dir.x);
        if (angle < 0f)
            angle += MathF.PI * 2f;

        float normalized = angle / (MathF.PI * 2f);

        return (byte)Math.Clamp((int)MathF.Round(normalized * 254f) + 1, 1, 255);
    }


    private string GetElapsedTime()
    {
        DateTime Now = DateTime.Now;
        TimeSpan Finish = Now - _startTime;

        // TotalMinutes'in tam kısmını al ve 2 haneli göster
        int minutes = (int)Finish.TotalMinutes;
        int seconds = Finish.Seconds;

        return $"{minutes:D2}:{seconds:D2}";
    }

    private float GetCurrentTime()
    {
        return TickManager.GetCurrentTime();
    }
}
