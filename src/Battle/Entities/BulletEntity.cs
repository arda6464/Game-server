using LiteEntitySystem;
using DietPhysics;

[EntityFlags(EntityFlags.UpdateOnClient | EntityFlags.Updateable)]
public class BulletEntity : PredictableEntityLogic
{
    [SyncVarFlags(SyncFlags.Interpolated)]
    public SyncVar<Vec3> Position;

    [SyncVarFlags(SyncFlags.Interpolated)]
    public SyncVar<Vec3> Direction;

    public SyncVar<float> Speed;
    public SyncVar<byte> Damage;
    public SyncVar<float> MaxRange;

    [SyncVarFlags(SyncFlags.SyncGroup1)]
    public SyncVar<EntitySharedReference> Shooter;

    private Vec3 _startPosition;
    private float _distanceTraveled;
    private bool _hasHit;

    public BulletEntity(EntityParams entityParams) : base(entityParams)
    {
    }

    public void Init(EntitySharedReference shooter, Vec3 position, Vec3 direction, float speed, byte damage, float range)
    {
        Position.Value = position;
        Direction.Value = direction;
        Speed.Value = speed;
        Damage.Value = damage;
        MaxRange.Value = range;
        Shooter.Value = shooter;
        _startPosition = position;
        _distanceTraveled = 0;
        _hasHit = false;
    }

    protected override void Update()
    {
        if (_hasHit) return;

        float delta = EntityManager.DeltaTimeF;
        float moveDist = Speed * delta;

        Position.Value = new Vec3(
            Position.Value.x + Direction.Value.x * moveDist,
            Position.Value.y + Direction.Value.y * moveDist,
            Position.Value.z + Direction.Value.z * moveDist
        );

        _distanceTraveled += moveDist;

        if (_distanceTraveled >= MaxRange)
        {
            Destroy();
        }
    }

    public void OnHit()
    {
        _hasHit = true;
        Destroy();
    }
}
