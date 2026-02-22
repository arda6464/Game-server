using System.Numerics;



public class Player
{
    public string? AccountId { get; set; }
    public string? Username { get; set; }
    public int AvatarId { get; set; }

    // Oyun içi değişkenler
    public Vector3 Position { get; set; }
    public Vector3 InputDirection { get; set; } // Server-side hareket için
    public float Speed { get; set; } = 5.0f; // Varsayılan hız

    public float Rotation { get; set; }
    public int Health { get; set; } = 100;
    public bool IsAlive { get; set; } = true;

    // Optimizasyon için
    public Vector3 LastSentPosition { get; set; }
    public float LastSentRotation { get; set; }
    
    // Ağ bağlantısı
    public Session? session;

    // Yetenekler, silahlar
    public string? CharacterId { get; set; }
    public int WeaponId { get; set; }
    public int BattleId { get; set;}
    public Vector3 StartPoint { get; set; }
    public int SpawnIndex { get; set; }


  
}
