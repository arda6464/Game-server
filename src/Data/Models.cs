/// <summary>
/// Karakter istatistik verileri.
/// characters.json dosyasından yüklenir.
/// </summary>///
using  DietPhysics;

public class CharacterData : GameData
{
    public int Hitpoints { get; set; }
    public float Speed { get; set; }
    public int AttackDamage { get; set; }
    public float AttackRange { get; set; }
}

/// <summary>
/// Mermi (Projectile) verileri.
/// projectiles.json dosyasından yüklenir.
/// </summary>
public class ProjectileData : GameData
{
    public float Speed { get; set; }
    public int Damage { get; set; }
    public float Range { get; set; }
    public float HitRadius { get; set; }
    public float DeathTime { get; set; } = 0; // Ne zaman öldü?
}

/// <summary>
/// Yerden alınabilir eşyaların (Silah, Can, Zırh vb.) şablon verisi.
/// </summary>
public class LootData : GameData
{
    public string Type { get; set; } // "Weapon", "Health", "Ammo"
    public int Value { get; set; }  // Can miktarı veya Silah ID'si
    public string ModelName { get; set; } // Client tarafındaki görsel karşılığı
}

public class LootItem
{
    public int LootId { get; set; }
    public int DataId { get; set; } // LootData ID'si
    public Vec3 Position { get; set; }
    public bool IsTaken { get; set; } = false;
    public float SpawnTime { get; set; }
}
