public class Gun
{
    // WeaponData'dan kopyalanan sabit bilgiler
    public int WeaponId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Damage { get; set; }
    public float ProjectileSpeed { get; set; }
    public float Range { get; set; }
    public float FireRate { get; set; }
    public float ReloadTime { get; set; }
    public int MaxAmmo { get; set; }
    public float PickupCooldown { get; set; }

    // Runtime state
    public int CurrentAmmo { get; set; }
    public int ReserveAmmo { get; set; }
    public bool IsReloading { get; set; }
    public float NextFireTime { get; set; }
    public float ReloadFinishTime { get; set; }

    public static Gun FromWeaponData(WeaponData weapon, bool fillMagazine = true)
    {
        Gun gun = new Gun();
        gun.ApplyDefinition(weapon, fillMagazine);
        return gun;
    }

    public void ApplyDefinition(WeaponData weapon, bool fillMagazine = true)
    {
        WeaponId = weapon.Id;
        Name = weapon.Name;
        Damage = weapon.Damage;
        ProjectileSpeed = weapon.Speed;
        Range = weapon.Range;
        FireRate = weapon.FireRate;
        ReloadTime = weapon.ReloadTime;
        MaxAmmo = weapon.MaxAmmo;
        PickupCooldown = weapon.CollectableTime;

        int magazineSize = Math.Max(1, weapon.MagazineSize);
        CurrentAmmo = fillMagazine ? magazineSize : 0;
        ReserveAmmo = Math.Max(0, MaxAmmo - CurrentAmmo);
        IsReloading = false;
        NextFireTime = 0;
        ReloadFinishTime = 0;
    }
}
