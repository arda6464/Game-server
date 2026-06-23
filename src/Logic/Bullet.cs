using DietPhysics;

public class Bullet
{

    public int BulletId { get; set; }
    public Vec3 Position { get; set; }
        public Vec3 Direction { get; set; }
    public DietSphere Collider {get;set;}
    public float Speed { get; set; }
    public int OwnerID { get; set; }
    public int Damage { get; set; }
    public float Range { get; set; }
    public Vec3 startPos { get; set; }
    public bool IsActive { get; set; } = true; // Aktif mi?
    public float DeathTime { get; set; } = 0; // Ne zaman öldü?
}