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
/// Server tarafindaki silah kural verileri.
/// weapons.json dosyasindan yuklenir.
/// </summary>
public class WeaponData : GameData
{
    public int ProjectileId { get; set; }
    public int Damage { get; set; }
    public float Speed { get; set; }
    public float FireRate { get; set; } // Saniyede atış sayısı
    public float Range { get; set; } // Merminin gidebileceği maksimum mesafe
    public int MagazineSize { get; set; } //  şarjör kapasitesi
    public int MaxAmmo { get; set; }
    public float ReloadTime { get; set; } 
    public float CollectableTime { get; set; }
}

/// <summary>
/// Yerden alınabilir eşyaların (Silah, Can, Zırh vb.) şablon verisi.
/// </summary>
/*public class LootData : GameData
{
    public string Type { get; set; } // "Weapon", "Health", "Ammo"
    public int Value { get; set; }  // Can miktarı veya Silah ID'si
    public string ModelName { get; set; } // Client tarafındaki görsel karşılığı
}*/

public enum LootItemType
{
   
    Weapon = 1
}

public class LootItem
{
    public int LootId { get; set; } // client'e gönderilen benzersiz ID
    public int DataId { get; set; } // LootData ID'si
    public LootItemType Type { get; set; }   // LootData ise Can, Ammo vb. olabilir, Weapon ise silah olarak değerlendirilecek
    public Vec3 Position { get; set; }
    public bool IsTaken { get; set; } = false;
    public float SpawnTime { get; set; }
}
public class PickupData
{
    public int PlayerId { get; set; }
    public int LootId { get; set; }
    public float PickupTime { get; set; }
}
