using DietPhysics;

public partial class BasePlayerEntity
{
    public int DatabaseId { get; set; }
    public Session? GameSession { get; set; }
    public Gun? ActiveGun { get; set; }
    public int KillCount { get; set; }
    public int DamageDealt { get; set; }
    public int HitCount { get; set; }
    public bool IsInBush { get; set; }
    public float AimAngle { get; set; }
    public float LastShotTime { get; set; } = -999f;
    public Vec3 AimDirection { get; set; }
    public DietSphere? Collider { get; set; }
}
