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
    public MapData? Map { get; set; }
    public BattleState State { get; private set; } = BattleState.WaitingToStart;
    
    public int BulletIdCounter = 0;
    public List<Player> Players { get; set; } = new List<Player>();
    public List<Bullet> Bullets { get; set; } = new List<Bullet>();
    
    private readonly object _lock = new object();
    private DateTime _startTime;

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
            foreach(var player in Players)
            {
                if (player.session != null)
                {
                    player.BattleId= 0;
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
                    bullet.Position += bullet.Direction * bullet.Speed;
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
            float deltaTime = 0.05f; // Fixed delta time for now (20Hz)

            foreach (var player in Players)
            {
                if (player.IsAlive && player.InputDirection != Vector3.Zero)
                {
                    player.Position += player.InputDirection * player.Speed * deltaTime;
                    if (player.session?.PlayerData != null)
                    {
                        player.session.PlayerData.Position = player.Position;
                    }
                }
            }
        }
    }

    public void UpdatePlayerPosition(string accountId, Vector3 newPos)
    {
        lock (_lock)
        {
            var player = Players.FirstOrDefault(p => p.AccountId == accountId);
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
                if (Vector3.Distance(pSource.Position, pSource.LastSentPosition) < 0.01f && 
                    Math.Abs(pSource.Rotation - pSource.LastSentRotation) < 0.5f)
                {
                    continue;
                }

                pSource.LastSentPosition = pSource.Position;
                pSource.LastSentRotation = pSource.Rotation;

                var packet = new PlayerMovePacket
                {
                    AccountId = pSource.AccountId,
                    X = pSource.Position.X,
                    Y = pSource.Position.Y,
                    Z = pSource.Position.Z,
                    Rotation = pSource.Rotation,
                    SequenceNumber = 0
                };

                foreach (var pTarget in Players)
                {
                    if (pTarget.AccountId != pSource.AccountId && pTarget.session?.UdpEndPoint != null)
                    {
                        using (ByteBuffer buffer = new ByteBuffer())
                        {
                          
                            packet.SequenceNumber = pTarget.session.GetNextSequence();
                            packet.Serialize(buffer);
                            pTarget.session.SendUnreliableUDP(buffer.ToArray());
                        }
                    }
                }


            }
        }
    }

    public Player? GetPlayer(string accountId)
    {
        lock (_lock)
        {
            return Players.FirstOrDefault(p => p.AccountId == accountId);
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

    public void RemovePlayer(string accountId)
    {
        lock (_lock)
        {
            Players.RemoveAll(p => p.AccountId == accountId);
            Logger.battlelog($"[BATTLE {BattleId}] Player removed: {accountId} (Remaining: {Players.Count})");
            
            if (Players.Count == 0 || (State == BattleState.Active && Players.Count(p => p.IsAlive) <= 1))
            {
                Stop();
            }
        }
    }

    public void OnPlayerDied(string deadPlayerId, string killerId)
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
