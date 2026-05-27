using DietPhysics;

public struct PendingInput
{
    public uint Tick;
    public Vec3 Direction;
}

public class Player
{
    public int ID { get; set; }
    public string? Username { get; set; }
    public int AvatarId { get; set; }

    // Oyun içi değişkenler
    public Vec3 Position { get; set; }
    public Vec3 InputDirection { get; set; } // Server-side hareket için (Eski sistem)
    public Queue<PendingInput> InputQueue { get; set; } = new Queue<PendingInput>();
    public uint LastProcessedTick { get; set; }
    public float Speed { get; set; } = 5.0f; // Varsayılan hız

    public float Rotation { get; set; }
    public int Health { get; set; } = 100;
    public bool IsAlive { get; set; } = true;

    // Optimizasyon için
    public Vec3 LastSentPosition { get; set; }
    public float LastSentRotation { get; set; }

    // Gecikme Telafisi (Lag Compensation) için geçmiş pozisyonlar
    public Dictionary<uint, Vec3> PositionHistory { get; set; } = new Dictionary<uint, Vec3>();

    // Ağ bağlantısı
    public Session? session;

    // Yetenekler, silahlar
    public string? CharacterId { get; set; }
    public int WeaponId { get; set; }
    public int BattleId { get; set; }
    public DietSphere? Collider { get; set; }
}
