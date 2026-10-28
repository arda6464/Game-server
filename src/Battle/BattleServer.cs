using System.Linq;
using DietPhysics;
using LiteEntitySystem;
using LiteEntitySystem.Transport;
using LiteNetLib;
using LiteNetLib.Utils;

public class BattleServer : IDisposable
{
    private readonly NetManager _netManager;
    private readonly ServerEntityManager _entityManager;
    private readonly Dictionary<int, LiteNetLibNetPeer> _peers = new();
    private readonly Dictionary<int, BasePlayerEntity> _playerEntities = new();
    private readonly Dictionary<int, int> _peerToPlayerId = new();
    private readonly List<BasePlayerEntity> _allPlayers = new();
    private readonly List<BulletEntity> _bullets = new();
    private readonly List<LootEntity> _loots = new();

    private class ActivePickup
    {
        public BasePlayerEntity Player;
        public LootEntity Loot;
        public float StartTime;
        public float Duration;
    }

    private readonly List<ActivePickup> _activePickups = new();

    private bool _running;
    private int _nextPlayerId = 1;
    private int _minPlayersToStart = 2;
    private BattleState _state = BattleState.WaitingToStart;
    private DietWorld _world = new();
    private List<Vec3> _spawnPoints;

    public int PlayerCount => _allPlayers.Count;
    public BattleState State => _state;

    private const float PlayerRadius = 0.5f;
    private const float LootSpawnRadius = 0.35f;
    private const int InitialWeaponSpawnCount = 10;
    private const float MoveSpeed = 5f;

    public BattleServer(int port)
    {
        EntityManager.RegisterFieldType<Vec3>(Vec3Lerp);

        try
        {
            MapManager.Load("Data/MapData.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Battle] MapData.json yuklenemedi: {ex.Message}");
        }

        var map = MapManager.LoadedMap;
        if (map != null)
        {
            foreach (WallData wall in map.walls)
            {
                _world.AddColliderStatic(
                    new DietBox(wall.pos, wall.center, wall.size, wall.rot, DietObjectType.Wall, 0)
                );
            }
            _world.Bake();
            _spawnPoints = map.spawnPoints;
        }

        var typesMap = new EntityTypesMap<GameEntities>()
            .Register(GameEntities.Player, e => new BasePlayerEntity(e))
            .Register(GameEntities.PlayerController, e => new PlayerInputController(e))
            .Register(GameEntities.Bullet, e => new BulletEntity(e))
            .Register(GameEntities.Loot, e => new LootEntity(e));

        _entityManager = new ServerEntityManager(
            typesMap,
            (byte)UdpMessageType.Input,
            30,
            ServerSendRate.EqualToFPS
        );

        var listener = new EventBasedNetListener();
        _netManager = new NetManager(listener)
        {
            AutoRecycle = true,
            UnconnectedMessagesEnabled = false,
            DisconnectTimeout = 30000,
        };

        listener.ConnectionRequestEvent += OnConnectionRequest;
        listener.PeerConnectedEvent += OnPeerConnected;
        listener.PeerDisconnectedEvent += OnPeerDisconnected;
        listener.NetworkReceiveEvent += OnNetworkReceive;

        _netManager.Start(port);
        _running = true;
        Console.WriteLine($"[BattleServer] LES baslatildi port={port}");
    }

    private static Vec3 Vec3Lerp(Vec3 a, Vec3 b, float t) =>
        new(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);

    private void OnConnectionRequest(ConnectionRequest request) => request.Accept();

    private void OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine($"[BattleServer] Peer connected: {peer.Id}");

        var lesPeer = new LiteNetLibNetPeer(peer, false);
        _peers[peer.Id] = lesPeer;

        var netPlayer = _entityManager.AddPlayer(lesPeer);
        peer.Tag = netPlayer;

        int playerId = _nextPlayerId++;
        var playerEntity = _entityManager.AddEntity<BasePlayerEntity>(e =>
        {
            e.PlayerId = playerId;
            e.PlayerName.Value = $"Player_{peer.Id}";
            int spawnIdx = _allPlayers.Count % (_spawnPoints?.Count ?? 1);
            Vec3 spawnPos =
                _spawnPoints != null && _spawnPoints.Count > 0
                    ? _spawnPoints[spawnIdx]
                    : new Vec3(0, 0, 0);
            e.Spawn(spawnPos);

            var collider = new DietSphere(
                spawnPos,
                Vec3.zero,
                PlayerRadius,
                DietObjectType.Player,
                playerId
            );
            e.Collider = collider;
            _world.AddColliderDynamic(collider);
        });

        _entityManager.AddController<PlayerInputController>(netPlayer, playerEntity);
        _playerEntities[playerId] = playerEntity;
        _peerToPlayerId[peer.Id] = playerId;
        _allPlayers.Add(playerEntity);

        Console.WriteLine(
            $"[BattleServer] Player {playerId} joined ({_allPlayers.Count}/{_minPlayersToStart})"
        );
    }

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"[BattleServer] Peer disconnected: {peer.Id}");
        RemovePlayerByPeerId(peer.Id);
    }

    private void RemovePlayerByPeerId(int peerId)
    {
        if (peerId <= 0)
            return;

        if (!_peerToPlayerId.TryGetValue(peerId, out int playerId))
            return;

        var toRemove = _playerEntities.GetValueOrDefault(playerId);
        if (toRemove != null)
        {
            if (toRemove.Collider != null)
                _world.RemoveColliderDynamic(toRemove.Collider);
            toRemove.Destroy();
            _allPlayers.Remove(toRemove);
            _playerEntities.Remove(playerId);
        }

        if (_peers.TryGetValue(peerId, out var lesPeer))
        {
            _entityManager.RemovePlayer(lesPeer);
            _peers.Remove(peerId);
        }

        _peerToPlayerId.Remove(peerId);
    }

    public void StartMatch(int expectedPlayers)
    {
        _minPlayersToStart = expectedPlayers;

        foreach (var pe in _playerEntities.Values)
        {
            if (pe.Collider != null)
                _world.RemoveColliderDynamic(pe.Collider);
            pe.Destroy();
        }
        _playerEntities.Clear();
        _allPlayers.Clear();
        _peerToPlayerId.Clear();

        foreach (var p in _peers.Values)
            _entityManager.RemovePlayer(p);
        _peers.Clear();

        foreach (var bullet in _bullets)
            bullet?.Destroy();
        _bullets.Clear();

        foreach (var loot in _loots)
            loot?.Destroy();
        _loots.Clear();

        _activePickups.Clear();
        _nextPlayerId = 1;
        _state = BattleState.WaitingToStart;
        Console.WriteLine($"[BattleServer] Match configured: {expectedPlayers} players expected");
    }

    public void RemovePeer(int peerId)
    {
        RemovePlayerByPeerId(peerId);
    }

    private void OnNetworkReceive(
        NetPeer peer,
        NetPacketReader reader,
        byte channel,
        DeliveryMethod deliveryMethod
    )
    {
        if (channel == 1)
        {
            byte[] data = reader.GetRemainingBytesSpan().ToArray();
            if (data.Length >= 2 && _peerToPlayerId.TryGetValue(peer.Id, out int playerId))
            {
                if (_playerEntities.TryGetValue(playerId, out var player))
                {
                    byte keys = data[0];
                    byte aimByte = data[1];
                    Vec3 moveDir = Vec3.zero;
                    if ((keys & (byte)InputKeys.Up) != 0) moveDir.z -= 1f;
                    if ((keys & (byte)InputKeys.Down) != 0) moveDir.z += 1f;
                    if ((keys & (byte)InputKeys.Left) != 0) moveDir.x -= 1f;
                    if ((keys & (byte)InputKeys.Right) != 0) moveDir.x += 1f;
                    player.PendingMovement = moveDir.sqrMagnitude > 0.001f ? moveDir.normalized : Vec3.zero;
                    player.PendingFire = (keys & (byte)InputKeys.Fire) != 0;
                    player.PendingAim = aimByte * (360f / 256f);
                }
            }
            return;
        }
        if (peer.Tag is NetPlayer netPlayer)
            _entityManager.Deserialize(netPlayer, reader.GetRemainingBytesSpan());
    }

    private void StartBattle()
    {
        _state = BattleState.Active;
        Console.WriteLine("[BattleServer] Battle started!");
        SpawnInitialLoots();
        Console.WriteLine($"[BattleServer] Spawned {_loots.Count} loot entities on map");
    }

    private int _logCounter;

    public void Update()
    {
        _netManager.PollEvents();
        _entityManager.Update();

        if (_state == BattleState.WaitingToStart && _allPlayers.Count >= _minPlayersToStart)
            StartBattle();

        if (_state != BattleState.Active)
            return;

        float dt = TickManager.instance?.DeltaTime ?? (1f / 30f);

        ProcessPlayerActions();
        UpdateBullets(dt);
        ProcessPickups(TickManager.GetCurrentTime());
        CheckMatchEnd();

        _logCounter++;
        if (_logCounter % 300 == 0)
        {
            foreach (var p in _allPlayers)
                Console.WriteLine($"[BattleServer] Player {p.PlayerId} pos={p.Position.Value.x:F1},{p.Position.Value.z:F1} health={p.Health} shield={p.Shield} alive={p.IsAlive}");
            Console.WriteLine($"[BattleServer] Loot count={_loots.Count}");
            foreach (var l in _loots)
                Console.WriteLine($"[BattleServer]   Loot {l.Id} type={l.ItemType} id={l.ItemId} pos={l.Position.Value.x:F1},{l.Position.Value.z:F1}");
        }
    }

    private void ProcessPlayerActions()
    {
        foreach (var player in _allPlayers)
        {
            if (!player.IsAlive)
                continue;

            if (player.PendingFire)
                PlayerFire(player);
        }
    }

    private void PlayerFire(BasePlayerEntity player)
    {
        if (player.PendingAim == 0)
            return;

        float angleRad = player.PendingAim * MathF.PI / 180f;
        Vec3 dir = new(MathF.Cos(angleRad), 0, MathF.Sin(angleRad));
        if (dir == Vec3.zero)
            return;

        Vec3 startPos = new(
            player.Position.Value.x + dir.x * (PlayerRadius + 0.1f),
            player.Position.Value.y,
            player.Position.Value.z + dir.z * (PlayerRadius + 0.1f)
        );

        byte aimByte = (byte)((player.PendingAim / 360f) * 255f);
        var refShooter = new EntitySharedReference(player);

        var bullet = _entityManager.AddEntity<BulletEntity>(e =>
        {
            e.Init(refShooter, startPos, dir, 20f, 25, 50f);
        });

        _bullets.Add(bullet);
        player.RpcPlayerShoot(player.PlayerId, aimByte, 0);
    }

    private void UpdateBullets(float dt)
    {
        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var bullet = _bullets[i];
            if (bullet == null || bullet.IsDestroyed)
            {
                _bullets.RemoveAt(i);
                continue;
            }

            Vec3 pos = bullet.Position.Value;
            Vec3 dir = bullet.Direction.Value;
            float moveDist = bullet.Speed * dt;
            Vec3 dirNorm = dir.normalized;

            var sweep = new DietSphere(pos, Vec3.zero, 0.05f, DietObjectType.None, 0);

            if (
                _world.SweepTest(
                    sweep,
                    dirNorm,
                    moveDist,
                    8,
                    out Vec3 hitPos,
                    out DietObjectType hitType,
                    out int hitData
                )
            )
            {
                if (hitType == DietObjectType.Player)
                {
                    var target = _playerEntities.GetValueOrDefault(hitData);
                    if (target != null && target.IsAlive)
                    {
                        var shooter = _entityManager.GetEntityById<BasePlayerEntity>(
                            bullet.Shooter.Value
                        );
                        if (shooter == null || target.PlayerId != shooter.PlayerId)
                        {
                            ApplyDamage(target, bullet, shooter);
                        }
                    }
                }
                _bullets.RemoveAt(i);
                bullet.Destroy();
                continue;
            }

            float traveled = Vec3.Distance(
                pos,
                new Vec3(pos.x - dirNorm.x * moveDist, pos.y, pos.z - dirNorm.z * moveDist)
            );

            if (traveled >= bullet.MaxRange)
            {
                _bullets.RemoveAt(i);
                bullet.Destroy();
            }
        }
    }

    private void ApplyDamage(BasePlayerEntity target, BulletEntity bullet, BasePlayerEntity shooter)
    {
        byte damage = bullet.Damage;
        target.Damage(damage);

        if (shooter != null)
            shooter.RpcHitConfirm(target.PlayerId, damage, target.Shield == 0);

        if (!target.IsAlive)
        {
            var killer =
                shooter ?? _entityManager.GetEntityById<BasePlayerEntity>(bullet.Shooter.Value);
            OnPlayerDied(target, killer);
        }
    }

    private void OnPlayerDied(BasePlayerEntity deadPlayer, BasePlayerEntity killer)
    {
        deadPlayer.Health.Value = 0;
        if (killer != null)
            killer.Kills++;

        deadPlayer.RpcPlayerDead(deadPlayer.PlayerId, killer?.PlayerId ?? 0, 0);

        int aliveCount = _allPlayers.Count(p => p.IsAlive);
        deadPlayer.RpcMatchResult(deadPlayer.PlayerId, aliveCount + 1, _allPlayers.Count, 0);

        SpawnDeathLoot(deadPlayer);
    }
    private void SpawnDeathLoot(BasePlayerEntity player)
    {
        float radius = 0.5f;
        int count = 4;
        byte[] itemTypes = { 0, 1, 2, 3 };
        for (int i = 0; i < count; i++)
        {
            float angle = i * (2f * MathF.PI / count);
            Vec3 pos = new(
                player.Position.Value.x + radius * MathF.Cos(angle),
                player.Position.Value.y,
                player.Position.Value.z + radius * MathF.Sin(angle)
            );

            int itemId = itemTypes[i] switch
            {
                0 => DataManager.GetAllWeapons().ElementAtOrDefault(Random.Shared.Next(DataManager.GetAllWeapons().Count))?.Id ?? 1,
                _ => Random.Shared.Next(0, 2) == 0 ? 25 : 50
            };
            SpawnLootEntity(pos, itemTypes[i], itemId);
        }
    }

    private void SpawnLootEntity(Vec3 position, byte itemType, int itemId)
    {
        var resolveSphere = new DietSphere(
            position,
            Vec3.zero,
            LootSpawnRadius,
            DietObjectType.None,
            0
        );
        if (_world.ResolveOverlap(resolveSphere, out Vec3 resolved))
            position = resolved;

        var loot = _entityManager.AddEntity<LootEntity>(e =>
        {
            e.Init(position, itemType, itemId);
        });
        _loots.Add(loot);
        Console.WriteLine($"[BattleServer] Spawned loot id={itemId} type={itemType} at ({position.x:F1},{position.z:F1})");
    }

    private void SpawnInitialLoots()
    {
        var weapons = DataManager.GetAllWeapons().ToArray();
        if (weapons.Length == 0)
        {
            Console.WriteLine("[BattleServer] No weapons found in DataManager, skipping initial loot spawn");
            return;
        }

        // Spawn loot at each player's position so they can pick it up immediately
        foreach (var player in _allPlayers)
        {
            Vec3 ppos = player.Position.Value;
            int weaponIdx = Random.Shared.Next(weapons.Length);
            SpawnLootEntity(new Vec3(ppos.x + 0.5f, ppos.y, ppos.z), 0, weapons[weaponIdx].Id);
            SpawnLootEntity(new Vec3(ppos.x - 0.5f, ppos.y, ppos.z), 1, 25);
        }

        int count = Math.Min(InitialWeaponSpawnCount, weapons.Length);
        var usedPoints = new HashSet<string>();

        for (int i = 0; i < count * 10 && usedPoints.Count < count; i++)
        {
            Vec3 pos = MapManager.GetRandomLootPoint();
            string key = $"{pos.x:F1}:{pos.y:F1}:{pos.z:F1}";
            if (usedPoints.Contains(key))
                continue;
            usedPoints.Add(key);

            int roll = Random.Shared.Next(4);
            byte type;
            int itemId;

            switch (roll)
            {
                case 0:
                    var weapon = weapons[Random.Shared.Next(weapons.Length)];
                    type = 0; itemId = weapon.Id;
                    break;
                case 1:
                    type = 1; itemId = Random.Shared.Next(0, 2) == 0 ? 25 : 50;
                    break;
                case 2:
                    type = 2; itemId = Random.Shared.Next(0, 2) == 0 ? 25 : 50;
                    break;
                default:
                    type = 3; itemId = Random.Shared.Next(0, 2) == 0 ? 25 : 50;
                    break;
            }

            SpawnLootEntity(pos, type, itemId);
        }
    }

    private void ProcessPickups(float currentTime)
    {
        foreach (var player in _allPlayers)
        {
            if (!player.IsAlive)
                continue;
            if (_activePickups.Any(p => p.Player == player))
                continue;

            foreach (var loot in _loots)
            {
                if (loot == null || loot.IsDestroyed)
                    continue;
                Vec3 ppos = player.Position.Value;
                Vec3 lpos = loot.Position.Value;
                float dist = Vec3.Distance(ppos, lpos);
                if (dist > 8f)
                    continue;
                Console.WriteLine(
                    $"[Pickup] Player {player.PlayerId} near loot {loot.Id} dist={dist:F2} itemType={loot.ItemType} itemId={loot.ItemId}"
                );

                byte itemType = loot.ItemType;
                if (itemType == 1 && player.Health >= 100)
                    continue;
                if (itemType == 2 && player.Shield >= 100)
                    continue;

                float duration = 0.1f;
                _activePickups.Add(
                    new ActivePickup
                    {
                        Player = player,
                        Loot = loot,
                        StartTime = currentTime,
                        Duration = duration,
                    }
                );
                player.RpcPickupStart(loot.Id, (int)(duration * 1000));
                break;
            }
        }

        for (int i = _activePickups.Count - 1; i >= 0; i--)
        {
            var pickup = _activePickups[i];
            if (pickup.Player == null || pickup.Loot == null || pickup.Loot.IsDestroyed)
            {
                _activePickups.RemoveAt(i);
                continue;
            }

            float dist = Vec3.Distance(pickup.Player.Position.Value, pickup.Loot.Position.Value);
            if (dist > 10f)
            {
                pickup.Player.RpcPickupCancel(pickup.Loot.Id);
                _activePickups.RemoveAt(i);
                continue;
            }

            if (currentTime - pickup.StartTime >= pickup.Duration)
            {
                CompletePickup(pickup);
                _activePickups.RemoveAt(i);
            }
        }
    }

    private void CompletePickup(ActivePickup pickup)
    {
        var player = pickup.Player;
        var loot = pickup.Loot;
        if (player == null || loot == null || loot.IsDestroyed)
            return;
        Console.WriteLine(
            $"[Pickup] Complete: player={player.PlayerId} loot={loot.Id} itemType={loot.ItemType} itemId={loot.ItemId}"
        );

        byte itemType = loot.ItemType;
        int itemId = loot.ItemId;

        switch (itemType)
        {
            case 1:
                player.Heal((byte)itemId);
                player.RpcItemChanged(0, itemType, itemId);
                break;
            case 2:
                player.AddShield((byte)itemId);
                player.RpcItemChanged(0, itemType, itemId);
                break;
            default:
                player.RpcItemChanged(0, itemType, itemId);
                break;
        }

        loot.Destroy();
        _loots.Remove(loot);
    }

    private void CheckMatchEnd()
    {
        int alive = _allPlayers.Count(p => p.IsAlive);
        if (alive <= 1 && _allPlayers.Count >= 2)
        {
            var winner = _allPlayers.FirstOrDefault(p => p.IsAlive);
            if (winner != null)
                winner.RpcMatchResult(winner.PlayerId, 1, _allPlayers.Count, 100);
            _state = BattleState.Finished;
            Console.WriteLine("[BattleServer] Battle finished!");
        }
    }

    public void Dispose()
    {
        _running = false;
        _netManager?.Stop();
    }
}
