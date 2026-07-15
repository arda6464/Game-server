using DietPhysics;

public enum BattleState
{
    WaitingToStart,
    Active,
    Finished
}

public partial class Battle
{
    public int BattleId { get; set; }

    public BattleState State { get; private set; } = BattleState.WaitingToStart;

    public int BulletIdCounter = 0;
    public int LootIdCounter = 0;
    private List<Player> _players = new List<Player>();
    private List<Bullet> _bullets = new List<Bullet>();
    private List<LootItem> _loots = new List<LootItem>();
    private List<PickupData> _pickups = new List<PickupData>();
    public DateTime StartedAt { get; private set; } = DateTime.MinValue;

    private readonly object _lock = new object();
    private DateTime _startTime;
    private DietWorld World = new DietWorld();
    private const float PlayerRadius = 0.5f;
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
            MapManager.Load("Data/MapData.json");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"[Battle] HATA: MapData.json yuklenemedi: {ex.Message}");
        }

        var map = MapManager.LoadedMap;

        foreach (WallData wall in map.walls)
        {
            DietBox box = new DietBox(wall.pos, wall.center, wall.size, wall.rot, DietObjectType.Wall, 0);
            World.AddColliderStatic(box);
            Console.WriteLine($"[Harita] Duvar eklendi: pos={box.GetPosition()} size={box.Size}");
        }

        World.Bake();

        PlayerSpawnPoints = map.spawnPoints;
        Console.WriteLine("----- Harita yuklendi -----");
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
        Update_bullets();
        BroadcastSnapshot();
    }

    public void AddBullet(Bullet bullet)
    {
        lock (_lock)
        {
            _bullets.Add(bullet);
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
            _bullets.RemoveAll(b => b.BulletId == bulletId);
        }
    }

    public Bullet? GetBullet(int id)
    {
        lock (_lock)
        {
            return _bullets.FirstOrDefault(b => b.BulletId == id);
        }
    }

    private void Update_bullets()
    {
        lock (_lock)
        {
            float currentTime = GetCurrentTime();
            foreach (var bullet in _bullets.ToList())
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
                    _bullets.Remove(bullet);
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
            int remainingShield = Targetplayer.Shield;
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

            foreach (var player in _players)
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

    private void UpdateAutoFire()
    {
        float now = GetCurrentTime();

        List<Player> snapshot;
        lock (_lock)
        {
            snapshot = _players.ToList();
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

        player.LastShotTime = GetCurrentTime();

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

            foreach (var player in _players)
            {
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
                        player.Position += direction * distance;
                    }
                    else
                    {
                        ApplyMovementWithSliding(player, direction, distance);
                    }

                    if (player.session?.PlayerData != null)
                        player.session.PlayerData.Position = player.Position;
                }

                player.PositionHistory[currentTick] = player.Position;

                uint oldTick = currentTick > (uint)TickManager.instance.TickRate
                    ? currentTick - (uint)TickManager.instance.TickRate
                    : 0;
                player.PositionHistory.Remove(oldTick);
            }
        }
    }

    private void ApplyMovementWithSliding(Player player, Vec3 direction, float distance)
    {
        int sweepIterations = Math.Max(5, (int)MathF.Ceiling(distance / Math.Max(PlayerRadius * 0.15f, 0.01f)));

        if (!World.SweepTest(player.Collider, direction, distance, sweepIterations, out _, out _, out _))
        {
            player.Position += direction * distance;
            player.Collider.Position = player.Position;
            return;
        }

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
            Console.WriteLine($"[Fizik] {player.Username} tamamen bloklandi.");
    }

    public void UpdatePlayerPosition(int id, Vec3 newPos)
    {
        lock (_lock)
        {
            var player = _players.FirstOrDefault(p => p.ID == id);
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
                    player.Position = collidedPos + delta.normalized * -0.01f;
                    Console.WriteLine($"[Fizik] {player.Username} paket ile duvara carpti, sinirda tutuldu.");
                }
                else
                {
                    player.Position = newPos;
                }

                player.Collider.Position = player.Position;
            }

            if (player.session?.PlayerData != null)
                player.session.PlayerData.Position = player.Position;

            player.CurrentBushId = GetBushIdAt(player.Position);
        }
    }

    private int? GetBushIdAt(Vec3 position)
    {
        var bushes = MapManager.LoadedMap?.bushes;
        if (bushes == null) return null;

        for (int i = 0; i < bushes.Count; i++)
        {
            var bush = bushes[i];
            float halfX = bush.size.x / 2f;
            float halfZ = bush.size.z / 2f;

            if (position.x >= bush.pos.x - halfX && position.x <= bush.pos.x + halfX &&
                position.z >= bush.pos.z - halfZ && position.z <= bush.pos.z + halfZ)
            {
                return i;
            }
        }
        return null;
    }

    public Player? GetPlayer(int id)
    {
        lock (_lock)
        {
            return _players.FirstOrDefault(p => p.ID == id);
        }
    }

    public List<Player> GetPlayers()
    {
        lock (_lock)
        {
            return _players.ToList();
        }
    }

    public List<LootItem> GetLoots()
    {
        lock (_lock)
        {
            return _loots.ToList();
        }
    }

    public void AddPlayer(Player player)
    {
        lock (_lock)
        {
            player.BattleId = BattleId;

            int spawnIndex = _players.Count % PlayerSpawnPoints.Count;
            player.Position = PlayerSpawnPoints[spawnIndex];

            if (player.session?.PlayerData != null)
                player.session.PlayerData.Position = player.Position;

            Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} spawned at {FormatVec3(player.Position)}");

            player.Collider = new DietSphere(player.Position, Vec3.zero, PlayerRadius, DietObjectType.Player, player.ID);
            Logger.battlelog($"[BATTLE {BattleId}] Player collider added: player={player.Username} pos={FormatVec3(player.Position)} radius={PlayerRadius:0.##}");
            World.AddColliderDynamic(player.Collider);
            RefreshActiveGunFromSelectedSlot(player);

            _players.Add(player);
            Logger.battlelog($"[BATTLE {BattleId}] Player added: {player.Username} (Total: {_players.Count})");
        }
    }

    public void RemovePlayer(int id)
    {
        lock (_lock)
        {
            var player = _players.FirstOrDefault(p => p.ID == id);
            if (player?.Collider != null)
                World.RemoveColliderDynamic(player.Collider);

            if (player?.session != null)
            {
                SessionManager.UnRegisterUdpSession(player.session.UdpEndPoint);
            }

            _players.RemoveAll(p => p.ID == id);
            Logger.battlelog($"[BATTLE {BattleId}] Player removed: {id} (Remaining: {_players.Count})");
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

        int placement, playerCount;
        lock (_lock)
        {
            placement = _players.Count(p => p.IsAlive) + 1;
            playerCount = _players.Count;
        }
        SendMatchResult(deadPlayer, false, placement, playerCount, 0);

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
            slot.Gun = null;
        }
        while (loots.Count < 3)
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

            var alivePlayers = _players.Where(p => p.IsAlive).ToList();
            if (alivePlayers.Count == 1)
            {
                SendMatchResult(alivePlayers[0], true, 1, _players.Count, 100);
                Stop();
            }
            else if (alivePlayers.Count == 0)
            {
                Stop();
            }
        }
    }
}
