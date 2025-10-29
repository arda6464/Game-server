using System.Numerics;

enum PlayerStatus
{
    Offline,
    Online,
    InQueue,
    InMatch
}

public class Player
{
    public string? AccountId { get; set; }
    public string? Username { get; set; }
    public int AvatarId { get; set; }

    // Oyun içi değişkenler
    public float PositionX { get; set; }
        public float PositionY { get; set; }
    public Vector2 Position { get; set; }

    public float Rotation { get; set; }
    public int Health { get; set; } = 100;
    public bool IsAlive { get; set; } = true;

    // Ağ bağlantısı
    public Session? session;

    // Yetenekler, silahlar
    public string? CharacterId { get; set; }
    public int WeaponId { get; set; }
    public int ArenaId { get; set;}

  
}
