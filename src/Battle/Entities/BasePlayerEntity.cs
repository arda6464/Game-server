using LiteEntitySystem;
using LiteEntitySystem.Extensions;
using DietPhysics;

[EntityFlags(EntityFlags.UpdateOnClient | EntityFlags.Updateable)]
public partial class BasePlayerEntity : PawnLogic
{
    [SyncVarFlags(SyncFlags.Interpolated | SyncFlags.LagCompensated | SyncFlags.SyncGroup1)]
    public SyncVar<Vec3> Position;

    [SyncVarFlags(SyncFlags.Interpolated | SyncFlags.SyncGroup1)]
    public SyncVar<byte> Health;

    [SyncVarFlags(SyncFlags.SyncGroup1)]
    public SyncVar<byte> Shield;

    [SyncVarFlags(SyncFlags.Interpolated | SyncFlags.SyncGroup1)]
    public SyncVar<byte> RotationByte;

    public readonly SyncString PlayerName = new();

    internal Vec3 PendingMovement;
    internal float PendingAim;
    internal bool PendingFire;
    internal bool PendingInteract;

    public int PlayerId;
    public int Kills;

    private static RemoteCall<HitConfirmData> _hitConfirmRpc;
    private static RemoteCall<PlayerDeadData> _playerDeadRpc;
    private static RemoteCall<ShootData> _playerShootRpc;
    private static RemoteCall<ItemSlotData> _itemChangedRpc;
    private static RemoteCall<ItemSlotData> _slotChangedRpc;
    private static RemoteCall<MatchResultData> _matchResultRpc;
    private static RemoteCall<PickupProgressData> _pickupStartRpc;
    private static RemoteCall<byte> _pickupCancelRpc;

    public struct HitConfirmData { public int TargetId; public int Damage; public bool ShieldHit; }
    public struct PlayerDeadData { public int DeadId; public int KillerId; public int GunId; }
    public struct ShootData { public int ShooterId; public byte AimByte; public int WeaponId; }
    public struct ItemSlotData { public int Slot; public int ItemType; public int DataId; }
    public struct MatchResultData { public int WinnerId; public int Placement; public int PlayerCount; public int Score; }
    public struct PickupProgressData { public int LootId; public int DurationMs; }

    public BasePlayerEntity(EntityParams entityParams) : base(entityParams)
    {
        Health.Value = 100;
    }

    public void Spawn(Vec3 position)
    {
        Position.Value = position;
        Health.Value = 100;
        Shield.Value = 0;
    }

    protected override void RegisterRPC(ref RPCRegistrator r)
    {
        base.RegisterRPC(ref r);
        r.CreateRPCAction(this, OnHitConfirm, ref _hitConfirmRpc, ExecuteFlags.SendToOwner);
        r.CreateRPCAction(this, OnPlayerDead, ref _playerDeadRpc, ExecuteFlags.SendToAll);
        r.CreateRPCAction(this, OnPlayerShoot, ref _playerShootRpc, ExecuteFlags.SendToAll);
        r.CreateRPCAction(this, OnItemChanged, ref _itemChangedRpc, ExecuteFlags.SendToAll);
        r.CreateRPCAction(this, OnSlotChanged, ref _slotChangedRpc, ExecuteFlags.SendToAll);
        r.CreateRPCAction(this, OnMatchResult, ref _matchResultRpc, ExecuteFlags.SendToOwner);
        r.CreateRPCAction(this, OnPickupStart, ref _pickupStartRpc, ExecuteFlags.SendToOwner);
        r.CreateRPCAction(this, OnPickupCancel, ref _pickupCancelRpc, ExecuteFlags.SendToOwner);
    }

    private void OnHitConfirm(HitConfirmData data) { }
    private void OnPlayerDead(PlayerDeadData data) { }
    private void OnPlayerShoot(ShootData data) { }
    private void OnItemChanged(ItemSlotData data) { }
    private void OnSlotChanged(ItemSlotData data) { }
    private void OnMatchResult(MatchResultData data) { }
    private void OnPickupStart(PickupProgressData data) { }
    private void OnPickupCancel(byte lootId) { }

    public void RpcHitConfirm(int targetId, int damage, bool shieldHit)
    {
        if (EntityManager.InNormalState)
            ExecuteRPC(_hitConfirmRpc, new HitConfirmData { TargetId = targetId, Damage = damage, ShieldHit = shieldHit });
    }

    public void RpcPlayerDead(int deadId, int killerId, int gunId)
    {
        if (EntityManager.InNormalState)
            ExecuteRPC(_playerDeadRpc, new PlayerDeadData { DeadId = deadId, KillerId = killerId, GunId = gunId });
    }

    public void RpcPlayerShoot(int shooterId, byte aimByte, int weaponId)
    {
        if (EntityManager.InNormalState)
            ExecuteRPC(_playerShootRpc, new ShootData { ShooterId = shooterId, AimByte = aimByte, WeaponId = weaponId });
    }

    public void RpcItemChanged(int slot, int itemType, int dataId)
    {
        if (EntityManager.InNormalState)
            ExecuteRPC(_itemChangedRpc, new ItemSlotData { Slot = slot, ItemType = itemType, DataId = dataId });
    }

    public void RpcSlotChanged(int slot, int itemType, int dataId)
    {
        if (EntityManager.InNormalState)
            ExecuteRPC(_slotChangedRpc, new ItemSlotData { Slot = slot, ItemType = itemType, DataId = dataId });
    }

    public void RpcMatchResult(int winnerId, int placement, int playerCount, int score)
    {
        if (EntityManager.InNormalState)
            ExecuteRPC(_matchResultRpc, new MatchResultData { WinnerId = winnerId, Placement = placement, PlayerCount = playerCount, Score = score });
    }

    public void RpcPickupStart(int lootId, int durationMs)
    {
        if (EntityManager.InNormalState)
            ExecuteRPC(_pickupStartRpc, new PickupProgressData { LootId = lootId, DurationMs = durationMs });
    }

    public void RpcPickupCancel(int lootId)
    {
        if (EntityManager.InNormalState)
            ExecuteRPC(_pickupCancelRpc, (byte)lootId);
    }

    public void Damage(byte amount)
    {
        if (amount >= Shield)
        {
            amount -= Shield;
            Shield.Value = 0;
            Health.Value = Health > amount ? (byte)(Health - amount) : (byte)0;
        }
        else
        {
            Shield.Value -= amount;
        }
    }

    public void Heal(byte amount)
    {
        int val = Health + amount;
        Health.Value = val > 100 ? (byte)100 : (byte)val;
    }

    public void AddShield(byte amount)
    {
        int val = Shield + amount;
        Shield.Value = val > 100 ? (byte)100 : (byte)val;
    }

    public bool IsAlive => Health > 0;

    public float Rotation
    {
        get => RotationByte.Value * (360f / 256f);
        set => RotationByte.Value = (byte)(value * (256f / 360f) % 256);
    }

    private const float MoveSpeed = 5f;

    protected override void Update()
    {
        if (PendingMovement.sqrMagnitude > 0.001f)
        {
            float dt = EntityManager.DeltaTimeF;
            Position.Value = new Vec3(
                Position.Value.x + PendingMovement.x * dt * MoveSpeed,
                Position.Value.y,
                Position.Value.z + PendingMovement.z * dt * MoveSpeed
            );

            if (Collider != null)
                Collider.Position = Position.Value;
        }

        if (PendingAim > 0)
        {
            Rotation = PendingAim;
        }
    }
}
