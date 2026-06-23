using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Tüm oyun verilerini tek yerden yöneten merkezi registry.
/// Program.cs içinde DataManager.Init() çağrılarak başlatılır.
///
/// Yeni bir veri türü nasıl eklenir?
///   1. GameData'dan türeyen yeni sınıfı Models.cs'e ekle.
///   2. Aşağıya yeni bir Dictionary ve Load metodu ekle.
///   3. Init() içinde yeni Load metodunu çağır.
/// </summary>
public static class DataManager
{
    // --- Sabit JSON seçenekleri (büyük/küçük harf duyarsız, verimli) ---
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    // --- Bellek içi tablolar ---
    private static Dictionary<int, CharacterData> _charactersById = new();
    private static Dictionary<string, CharacterData> _charactersByName = new(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<int, ProjectileData> _projectilesById = new();
    private static Dictionary<string, ProjectileData> _projectilesByName = new(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<int, WeaponData> _weaponsById = new();
    private static Dictionary<string, WeaponData> _weaponsByName = new(StringComparer.OrdinalIgnoreCase);

    private static readonly List<LootData> _lootItems = new();
    private static readonly Dictionary<string, LootData> _lootItemsByKey = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, LootData> _lootItemsByName = new(StringComparer.OrdinalIgnoreCase);

    // ─────────────────────────────────────────────────────────────────
    // BAŞLATMA
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tüm JSON veri dosyalarını bellege yükler.
    /// Program başlangıcında bir kez çağrılmalıdır.
    /// </summary>
    public static void Init(string dataFolder = "Data")
    {
        LoadCollection<CharacterData>(Path.Combine(dataFolder, "characters.json"),
            ref _charactersById, ref _charactersByName);

        LoadCollection<ProjectileData>(Path.Combine(dataFolder, "projectiles.json"),
            ref _projectilesById, ref _projectilesByName);

        LoadCollection<WeaponData>(Path.Combine(dataFolder, "weapons.json"),
            ref _weaponsById, ref _weaponsByName);

        LoadLootItems(Path.Combine(dataFolder, "loot_items.json"));

        if (_weaponsById.Count == 0)
        {
            Console.WriteLine("[DataManager] UYARI: Silah verisi yüklenmedi. weapons.json boş veya okunamıyor olabilir.");
        }
        else
        {
            Console.WriteLine($"[DataManager] Güvenlik kontrolü: {_weaponsById.Count} silah yüklendi.");
        }

       
        Console.WriteLine($"[DataManager] Yükleme tamamlandı. " +
                          $"Karakter: {_charactersById.Count}, " +
                          $"Mermi: {_projectilesById.Count}, " +
                        $"Silah: {_weaponsById.Count}, " +
                        $"Loot: {_lootItems.Count}, ");
                         
    }

    // ─────────────────────────────────────────────────────────────────
    // ERİŞİM METODları
    // ─────────────────────────────────────────────────────────────────

    /// <summary>ID ile karakter verisini getirir. Bulunamazsa null döner.</summary>
    public static CharacterData? GetCharacter(int id)
        => _charactersById.GetValueOrDefault(id);

    /// <summary>İsim ile karakter verisini getirir. Büyük/küçük harf duyarsız.</summary>
    public static CharacterData? GetCharacter(string name)
        => _charactersByName.GetValueOrDefault(name);

    /// <summary>ID ile mermi verisini getirir. Bulunamazsa null döner.</summary>
    public static ProjectileData? GetProjectile(int id)
        => _projectilesById.GetValueOrDefault(id);

    /// <summary>İsim ile mermi verisini getirir. Büyük/küçük harf duyarsız.</summary>
    public static ProjectileData? GetProjectile(string name)
        => _projectilesByName.GetValueOrDefault(name);

    /// <summary>ID ile silah verisini getirir. Bulunamazsa null döner.</summary>
    public static WeaponData? GetWeapon(int id)
        => _weaponsById.GetValueOrDefault(id);

    /// <summary>İsim ile silah verisini getirir. Büyük/küçük harf duyarsız.</summary>
    public static WeaponData? GetWeapon(string name)
        => _weaponsByName.GetValueOrDefault(name);

    public static IReadOnlyCollection<WeaponData> GetAllWeapons()
        => _weaponsById.Values;

    /// <summary>Tip ve id ile loot item verisini getirir. Bulunamazsa null döner.</summary>
    public static LootData? GetLootItem(LootItemType type, int id)
        => _lootItemsByKey.GetValueOrDefault(BuildLootKey(type, id));

    /// <summary>İsim ile loot item verisini getirir. Büyük/küçük harf duyarsız.</summary>
    public static LootData? GetLootItem(string name)
        => _lootItemsByName.GetValueOrDefault(name);

    public static IReadOnlyCollection<LootData> GetAllLootItems()
        => _lootItems;


    // ─────────────────────────────────────────────────────────────────
    // YARDIMCI
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generic yükleme motoru.
    /// Herhangi bir GameData türevi JSON dosyasını okuyup iki Dictionary'ye doldurur.
    /// </summary>
    private static void LoadCollection<T>(
        string filePath,
        ref Dictionary<int, T> byId,
        ref Dictionary<string, T> byName)
        where T : GameData
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[DataManager] UYARI: Dosya bulunamadı → {filePath}");
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            List<T>? items = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);

            if (items == null) return;

            byId   = new Dictionary<int, T>(items.Count);
            byName = new Dictionary<string, T>(items.Count, StringComparer.OrdinalIgnoreCase);

            foreach (T item in items)
            {
                byId[item.Id]     = item;
                byName[item.Name] = item;
            }

            Console.WriteLine($"[DataManager] '{Path.GetFileName(filePath)}' → {items.Count} kayıt yüklendi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DataManager] HATA: '{filePath}' yüklenirken → {ex.Message}");
        }
    }

    private static void LoadLootItems(string filePath)
    {
        _lootItems.Clear();
        _lootItemsByKey.Clear();
        _lootItemsByName.Clear();

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[DataManager] UYARI: Dosya bulunamadı → {filePath}");
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            List<LootData>? items = JsonSerializer.Deserialize<List<LootData>>(json, _jsonOptions);
            if (items == null) return;

            foreach (LootData item in items)
            {
                string key = BuildLootKey(item.Type, item.Id);
                _lootItems.Add(item);
                _lootItemsByKey[key] = item;
                _lootItemsByName[item.Name] = item;
            }

            Console.WriteLine($"[DataManager] '{Path.GetFileName(filePath)}' → {items.Count} kayıt yüklendi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DataManager] HATA: '{filePath}' yüklenirken → {ex.Message}");
        }
    }

    private static string BuildLootKey(LootItemType type, int id)
        => $"{(int)type}:{id}";
}
