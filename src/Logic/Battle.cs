using System.Numerics;
using Logic;

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
    public List<Player> Players { get; set; } = new List<Player>();
    public List<Bullet> Bullets { get; set; } = new List<Bullet>();

    private readonly object _lock = new object();
    private DateTime _startTime;
    public List<Vector3> SpawnPoints = new List<Vector3>
    {
        new Vector3(11,1,-8),
        new Vector3(11,1,17),
        new Vector3(40,1,16),
        new Vector3(41,1,-9)
    };


    public void Start()
    {
        lock (_lock)
        {
            if (State != BattleState.WaitingToStart) return;
            State = BattleState.Active;
            _startTime = DateTime.Now;
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

            // Cleanup players' arena reference
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
                    bullet.Position += bullet.Direction * bullet.Speed * TickManager.instance.DeltaTime * 20f; // *20f to keep original feel if Speed was units/tick @ 20Hz
                    float traveledDistance = Vector2.Distance(bullet.startPos, bullet.Position);

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
                if (player.IsAlive && player.InputDirection != Vector3.Zero)
                {
                    Vector3 direction = Vector3.Normalize(player.InputDirection);
                    player.Position += direction * player.Speed * deltaTime;
                    if (player.session?.PlayerData != null)
                    {
                        player.session.PlayerData.Position = player.Position;
                        player.session.PlayerData.InputDirection = Vector3.Zero;
                    }
                }

                // --- HAFIZA SİSTEMİ (HISTORY BUFFER) ---
                // Oyuncunun bu tick'teki pozisyonunu kaydet
                player.PositionHistory[currentTick] = player.Position;

                // Eski pozisyonları sil (maksimum 1 saniye geriye bakılmasına izin verilir)
                if (currentTick > TickManager.instance.TickRate)
                {
                    player.PositionHistory.Remove(currentTick - (uint)TickManager.instance.TickRate);
                }
            }
        }
    }

    public void UpdatePlayerPosition(int id, Vector3 newPos)
    {
        lock (_lock)
        {
            var player = Players.FirstOrDefault(p => p.ID == id);
            if (player != null)
            {
                player.Position = newPos;
                if (player.session?.PlayerData != null)
                {
                    player.session.PlayerData.Position = newPos;
                }
            }
        }
    }

    public void BroadcastSnapshot()
    {
        lock (_lock)
        {
            foreach (var pSource in Players)
            {
                /* if (Vector3.Distance(pSource.Position, pSource.LastSentPosition) < 0.01f &&
                     Math.Abs(pSource.Rotation - pSource.LastSentRotation) < 0.5f)
                 {
                     Console.WriteLine("broadcast'te contue edildi");
                     continue;
                 }*/

                pSource.LastSentPosition = pSource.Position;
                pSource.LastSentRotation = pSource.Rotation;

                var packet = new PlayerMovePacket
                {
                    Tick = TickManager.instance.Get_Tick(),
                    ID = pSource.ID,
                    X = pSource.Position.X,
                    Y = pSource.Position.Y,
                    Z = pSource.Position.Z,
                };

                byte[] payloadData;
                using (ByteBuffer payloadBuffer = new ByteBuffer())
                {
                    packet.Serialize(payloadBuffer);
                    payloadData = payloadBuffer.ToArray();
                }

                foreach (var pTarget in Players)
                {
                    if (/*pTarget.AccountId != pSource.AccountId &&*/ pTarget.session?.UdpEndPoint != null)
                    {
                        pTarget.session.SendUnreliableUDP_Payload(payloadData);
                    }
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

    public void AddPlayer(Player player)
    {
        lock (_lock)
        {
            player.BattleId = BattleId;
            Players.Add(player);
            Logger.battlelog($"[BATTLE {BattleId}] Player added: {player.Username} (Total: {Players.Count})");
        }
    }

    public void RemovePlayer(int id)
    {
        lock (_lock)
        {
            Players.RemoveAll(p => p.ID == id);
            Logger.battlelog($"[BATTLE {BattleId}] Player removed: {id} (Remaining: {Players.Count})");

            if (Players.Count == 0 || (State == BattleState.Active && Players.Count(p => p.IsAlive) <= 1))
            {
                Stop();
            }
        }
    }

    public void OnPlayerDied(int deadPlayerId, int killerId)
    {
        var deadPlayer = GetPlayer(deadPlayerId);
        if (deadPlayer != null)
        {
            deadPlayer.IsAlive = false;
        }

        var packet = new PlayerDeadPacket
        {
            DeadPlayerId = deadPlayerId,
            KillerId = killerId
        };

        foreach (var player in GetPlayers())
        {
            player.session?.Send(packet);
        }

        CheckMatchEnd();
    }

    private void CheckMatchEnd()
    {
        lock (_lock)
        {
            if (State != BattleState.Active) return;

            var alivePlayers = Players.Where(p => p.IsAlive).ToList();
            /*  if (alivePlayers.Count <= 1)
              {
                  // TODO: Victory/Defeat packets
                  Stop();
              }*/
        }
    }

    private float GetCurrentTime()
    {
        return Environment.TickCount / 1000f;
    }
}
