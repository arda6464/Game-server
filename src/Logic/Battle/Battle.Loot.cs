using DietPhysics;

partial class Battle
{
    public int GetNextLootId()
    {
        return Interlocked.Increment(ref LootIdCounter);
    }

    public bool SpawnLoot(LootItemType type, int dataId, Vec3 position)
    {
        bool spawned = TrySpawnLoot(type, dataId, position);
        return spawned;
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
            _loots.Add(loot);
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
                LootData lootData = GetRandomLootData(lootitem, random);
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

    private LootData GetRandomLootData(LootItemType type, Random random)
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
            _loots.Add(loot);
            Logger.battlelog($"[BATTLE {BattleId}] Loot spawned: {dataId} at {FormatVec3(position)} (LootId: {loot.LootId})");

            return true;
        }
    }

    public LootItem ForceSpawnLoot(LootItemType type, int dataId, Vec3 position)
    {
        lock (_lock)
        {
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
            _loots.Add(loot);
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

        foreach (Player player in _players)
        {
            if (Vec3.Distance(position, player.Position) < MinLootDistanceFromPlayer)
            {
                reason = $"too close to player {player.ID}";
                return false;
            }
        }

        foreach (LootItem loot in _loots)
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
        LootItem loot;
        lock (_lock)
        {
            loot = _loots.FirstOrDefault(l => l.LootId == lootId);
        }

        if (player == null || loot == null)
            return;

        bool hasPickup;
        lock (_lock)
        {
            hasPickup = _pickups.Any(p => p.PlayerId == playerId || p.LootId == lootId);
        }
        if (hasPickup)
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

        lock (_lock)
        {
            _pickups.Add(new PickupData
            {
                PlayerId = playerId,
                LootId = lootId,
                PickupTime = now,
                FinishTime = now + collectTime,
                RequiredTime = collectTime
            });
        }
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
        PickupData[] snapshot;
        lock (_lock)
        {
            snapshot = _pickups.ToArray();
        }
        foreach (var pickup in snapshot)
        {
            var player = GetPlayer(pickup.PlayerId);
            LootItem loot;
            lock (_lock)
            {
                loot = _loots.FirstOrDefault(l => l.LootId == pickup.LootId);
            }
            if (player == null || loot == null)
            {
                lock (_lock) { _pickups.Remove(pickup); }
                continue;
            }

            if (Vec3.Distance(player.Position, loot.Position) > 1.0f)
            {
                Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} moved away from loot {loot.LootId}, pickup cancelled");
                lock (_lock) { _pickups.Remove(pickup); }
                continue;
            }

            if (currentTime < pickup.FinishTime)
                continue;

            BroadcastLootDeletion(loot.LootId);

            lock (_lock) { _pickups.Remove(pickup); }
            GiveItemToPlayer(player, loot);
            Logger.battlelog($"[BATTLE {BattleId}] Player {player.Username} picked up loot {loot.LootId} after {pickup.RequiredTime:0.##}s");
        }
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
}
