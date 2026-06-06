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


    private readonly object _lock = new object();
    private DateTime _startTime;
    private DietWorld World = new DietWorld();
    private const float PlayerRadius = 0.5f; // 
    private const float LootSpawnRadius = 0.35f;
    private const float MinLootDistanceFromPlayer = 1.0f;
    private const float MinLootDistanceFromLoot = 1.0f;
    private const int InitialWeaponSpawnCount = 4;
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
            DietBox box = new DietBox(wall.pos, wall.center, wall.size, wall.rot);
            World.AddColliderStatic(box);
            Console.WriteLine($"[Harita] Duvar eklendi: pos={box.GetPosition()} size={box.Size}");
        }

        // Statik collider'ları pişir (spatial optimizasyon için).
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
            SpawnInitialWeapons();
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
                }
            }

            ArenaManager.RemoveBattle(BattleId);
        }
    }

    public void Tick()
    {
        if (State != BattleState.Active) return;

        UpdatePlayerPositions();
        UpdatePickups();
        UpdateBullets();
        BroadcastSnapshot();
        CheckMatchEnd();
    }

    public void AddBullet(Bullet bullet)
    {
        lock (_lock)
        {
            Bullets.Add(bullet);
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
                    bullet.Position += bullet.Direction * bullet.Speed * TickManager.instance.DeltaTime;
                    float traveledDistance = Vec3.Distance(bullet.startPos, bullet.Position);

                    if (traveledDistance >= bullet.menzil)
                    {
                        bullet.IsActive = false;
                        bullet.DeathTime = currentTime;
                    }
                }

                if (!bullet.IsActive && (currentTime - bullet.DeathTime) > 3.0f)
                {
                    Bullets.Remove(bullet);
                }
            }
        }
    }

    private void UpdatePlayerPositions()
    {
        lock (_lock)
        {
            float deltaTime = TickManager.instance.DeltaTime;
            uint currentTick = TickManager.instance.Get_Tick();

            foreach (var player in Players)
            {
                // Depenetration: Oyuncu bir nesnenin içindeyse dışarı it.
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
                        // Collider yoksa fizik kontrolü yapma, doğrudan hareket et.
                        player.Position += direction * distance;
                    }
                    else
                    {
                        ApplyMovementWithSliding(player, direction, distance);
                    }

                    if (player.session?.PlayerData != null)
                        player.session.PlayerData.Position = player.Position;
                }

                // Bu tick'teki pozisyonu kayıt et.
                player.PositionHistory[currentTick] = player.Position;

                // 1 saniyeden eski pozisyon kayıtlarını temizle.
                uint oldTick = currentTick > (uint)TickManager.instance.TickRate
                    ? currentTick - (uint)TickManager.instance.TickRate
                    : 0;
                player.PositionHistory.Remove(oldTick);
            }
        }
    }

    /// <summary>
    /// Önce tam yönde hareket dene, engel varsa X ve Z eksenlerinde ayrı ayrı kayma dene.
    /// </summary>
    private void ApplyMovementWithSliding(Player player, Vec3 direction, float distance)
    {
        int sweepIterations = Math.Max(5, (int)MathF.Ceiling(distance / Math.Max(PlayerRadius * 0.15f, 0.01f)));

        // Tam hareket mümkünse direkt ilerle.
        if (!World.SweepTest(player.Collider, direction, distance, sweepIterations, out _))
        {
            player.Position += direction * distance;
            player.Collider.Position = player.Position;
            return;
        }

        // Engel var — eksen bazlı kayma dene.
        bool movedX = false;
        bool movedZ = false;

        if (MathF.Abs(direction.x) > 0.1f)
        {
            Vec3 xDir = new Vec3(direction.x, 0, 0).normalized;
            float xDist = distance * MathF.Abs(direction.x);
            if (!World.SweepTest(player.Collider, xDir, xDist, sweepIterations, out _))
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
            if (!World.SweepTest(player.Collider, zDir, zDist, sweepIterations, out _))
            {
                player.Position += zDir * zDist;
                player.Collider.Position = player.Position;
                movedZ = true;
            }
        }

        if (!movedX && !movedZ)
            Console.WriteLine($"[Fizik] {player.Username} tamamen bloklandı.");
    }

    /// <summary>
    /// Sunucuya gelen ham pozisyon paketini fizik doğrulamasından geçirir.
    /// Geçersizse oyuncuyu sınır noktasına çeker.
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

                if (distance > 0.001f && World.SweepTest(player.Collider, delta.normalized, distance, sweepIterations, out Vec3 collidedPos))
                {
                    // Collision tespit edildi: collider yüzeyinin biraz gerisine al.
                    player.Position = collidedPos + delta.normalized * -0.01f;
                    Console.WriteLine($"[Fizik] {player.Username} paket ile duvara çarptı, sınırda tutuldu.");
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

                byte[] payloadData;
                using (ByteBuffer payloadBuffer = ByteBufferPool.Get())
                {
                    packet.Serialize(payloadBuffer);
                    payloadData = payloadBuffer.ToArray();
                }

                foreach (var pTarget in Players)
                {
                    if (pTarget.session?.UdpEndPoint != null)
                        pTarget.session.SendUnreliableUDP_Payload(payloadData);
                }
            }
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

            Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} spawned at {player.Position}");

            // Oyuncu collider'ı dynamic olarak ekleniyor (oyuncular hareket eden objeler).
            player.Collider = new DietSphere(player.Position, Vec3.zero, PlayerRadius);
            World.AddColliderDynamic(player.Collider);

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

            Players.RemoveAll(p => p.ID == id);
            Logger.battlelog($"[BATTLE {BattleId}] Player removed: {id} (Remaining: {Players.Count})");

            if (Players.Count == 0 || (State == BattleState.Active && Players.Count(p => p.IsAlive) <= 1))
                Stop();
        }
    }

    public void OnPlayerDied(int deadPlayerId, int killerId)
    {
        var deadPlayer = GetPlayer(deadPlayerId);
        if (deadPlayer != null)
            deadPlayer.IsAlive = false;

        var packet = new PlayerDeadPacket
        {
            DeadPlayerId = deadPlayerId,
            KillerId = killerId
        };

        foreach (var player in GetPlayers())
            player.session?.Send(packet);

        CheckMatchEnd();
    }

    private void CheckMatchEnd()
    {
        lock (_lock)
        {
            if (State != BattleState.Active) return;

            var alivePlayers = Players.Where(p => p.IsAlive).ToList();
            // if (alivePlayers.Count <= 1) Stop();
        }
    }

    public int GetNextLootId()
    {
        return Interlocked.Increment(ref LootIdCounter);
    }

    public void SpawnLoot(LootItemType type, int dataId, Vec3 position)
    {
        TrySpawnLoot(type, dataId, position);
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

            if (!IsValidLootSpawnPosition(position, out string reason))
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

    public bool TrySpawnLoot(LootItemType type, int dataId, Vec3 position) //  dataid: 
    {
        lock (_lock)
        {
            if (!IsValidLootData(dataId))
                return false;

            if (!IsValidLootSpawnPosition(position, out string reason))
            {
                Logger.battlelog($"[BATTLE {BattleId}] Loot spawn rejected: {dataId} at {FormatVec3(position)}. Reason: {reason}");
                return false;
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
            Logger.battlelog($"[BATTLE {BattleId}] Loot spawned: {dataId} at {FormatVec3(position)}");
            return true;
        }
    }

    private bool IsValidLootData(int dataId)
    {
        /* LootData? lootData = DataManager.GetLoot(dataId);
         if (lootData == null)
         {
             Logger.battlelog($"[BATTLE {BattleId}] Loot spawn rejected: data not found ({dataId})");
             return false;
         }

         if (string.Equals(lootData.Type, "Weapon", StringComparison.OrdinalIgnoreCase)
             && DataManager.GetWeapon(lootData.Value) == null)
         {
             Logger.battlelog($"[BATTLE {BattleId}] Loot spawn rejected: weapon not found ({lootData.Value})");
             return false;
         }

         return true;*/
        return true; // Şimdilik tüm dataId'leri geçerli sayıyoruz, çünkü loot data yapısını henüz tanımlamadık.
    }

    private bool IsValidLootSpawnPosition(Vec3 position, out string reason)
    {
        DietSphere testSphere = new DietSphere(position, Vec3.zero, LootSpawnRadius);
        if (World.ResolveOverlap(testSphere, out _))
        {
            reason = "wall/player collision";
            return false;
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

    private void SpawnInitialWeapons()
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

            WeaponData weapon = weapons[Random.Shared.Next(weapons.Length)];
            Vec3 position = MapManager.GetRandomLootPoint();
            string pointKey = $"{position.x:F1}:{position.y:F1}:{position.z:F1}";

            if (usedPoints.Contains(pointKey))
                continue;

            if (TrySpawnWeapon(weapon.Id, position))
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

        WeaponData? weapon = DataManager.GetWeapon(loot.DataId);
        float collectTime = weapon?.CollectableTime ?? 1.0f;
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

            if (loot.Type == LootItemType.Weapon)
                player.WeaponId = loot.DataId;
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
        player.SelectedSlot = targetSlot;

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
    public void ChangePlayerSlot(int playerId, int toSlot)
    {
        var player = GetPlayer(playerId);
        if (player == null) return;

        if (toSlot < 0 || toSlot >= player.InventorySlots.Count || player.SelectedSlot == toSlot)
            return;


        player.SelectedSlot = toSlot;

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

    private float GetCurrentTime()
    {
        return Environment.TickCount / 1000f;
    }
}
