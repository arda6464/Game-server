using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

public static class ShopManager
{
    private static List<MarketItemData> BaseMarketOffer = new List<MarketItemData>();
    private static Dictionary<int, MarketOfferData> ActiveOffers = new Dictionary<int, MarketOfferData>();
    private static Dictionary<string, DateTime> PlayerPurchaseHistory = new Dictionary<string, DateTime>();
    
    private static readonly string _itemsPath = "market_items.json";
    private static readonly string _offersPath = "market_offers.json";
    private static readonly object _lock = new object();

    public static DateTime ExpiresAt { get; private set; }
    public static TimeSpan RefreshInterval { get; private set; } = TimeSpan.FromHours(24);

    static ShopManager()
    {
        Load();
    }

    // Market verilerini yükle
    public static void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_itemsPath))
                {
                    var json = File.ReadAllText(_itemsPath);
                    BaseMarketOffer = JsonConvert.DeserializeObject<List<MarketItemData>>(json) ?? new List<MarketItemData>();
                }

                if (File.Exists(_offersPath))
                {
                    var json = File.ReadAllText(_offersPath);
                    var list = JsonConvert.DeserializeObject<List<MarketOfferData>>(json) ?? new List<MarketOfferData>();
                    ActiveOffers = list.ToDictionary(o => o.OfferId, o => o);
                }

                if (BaseMarketOffer.Count == 0 && !File.Exists(_itemsPath)) GenerateDefaults();
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[ShopManager] Load Hatası: {ex.Message}");
            }
        }
    }

    // Market verilerini kaydet
    public static void Save()
    {
        lock (_lock)
        {
            try
            {
                File.WriteAllText(_itemsPath, JsonConvert.SerializeObject(BaseMarketOffer, Formatting.Indented));
                File.WriteAllText(_offersPath, JsonConvert.SerializeObject(ActiveOffers.Values.ToList(), Formatting.Indented));
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[ShopManager] Save Hatası: {ex.Message}");
            }
        }
    }

    private static void GenerateDefaults()
    {
        BaseMarketOffer.Add(new MarketItemData { ItemId = 1001, ItemName = "Küçük Elmas Paketi", ItemType = ItemType.Gems, PriceType = PriceType.RealMoney, BasePrice = 10, Count = 100 });
        BaseMarketOffer.Add(new MarketItemData { ItemId = 1002, ItemName = "Büyük Elmas Paketi", ItemType = ItemType.Gems, PriceType = PriceType.RealMoney, BasePrice = 50, Count = 600 });
        Save();
    }

    // Market başlatma
    public static void InitializeMarket()
    {
        // Artık statik yükleme yapıyoruz
        if (IsExpired())
        {
            SetExpiration();
        }
    }

    public static void AddItem(MarketItemData item)
    {
        lock (_lock)
        {
            if (item.ItemId == 0) item.ItemId = GenerateUniqueId();
            BaseMarketOffer.Add(item);
            Save();
        }
    }

    public static void RemoveItem(int itemId)
    {
        lock (_lock)
        {
            BaseMarketOffer.RemoveAll(i => i.ItemId == itemId);
            Save();
        }
    }

    public static void AddOffer(MarketOfferData offer)
    {
        lock (_lock)
        {
            if (offer.OfferId == 0) offer.OfferId = GenerateUniqueId();
            ActiveOffers[offer.OfferId] = offer;
            Save();
        }
    }

    public static void RemoveOffer(int offerId)
    {
        lock (_lock)
        {
            ActiveOffers.Remove(offerId);
            Save();
        }
    }

    // Süre dolumunu ayarla
    private static void SetExpiration()
    {
        ExpiresAt = DateTime.UtcNow.Add(RefreshInterval);
    }

    // Süre doldu mu kontrolü
    public static bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }

    // Market ürünlerini getir
    public static List<MarketItemData> GetMarketItems(string playerId = "")
    {
        if (!DynamicConfigManager.Config.IsShopEnabled) return new List<MarketItemData>();
        return BaseMarketOffer;
    }

    public static List<MarketOfferData> GetOffers(string playerId = "")
    {
        if (!DynamicConfigManager.Config.IsShopEnabled) return new List<MarketOfferData>();
        
        // Süresi biten teklifleri temizle
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var expiredIds = ActiveOffers.Values.Where(o => o.EndTime < now).Select(o => o.OfferId).ToList();
            if (expiredIds.Count > 0)
            {
                foreach (var id in expiredIds) ActiveOffers.Remove(id);
                Save();
            }
        }
        
        return ActiveOffers.Values.ToList();
    }

    public static void RefreshMarket() => Load();

    public static void Update()
    {
        if (IsExpired())
        {
            RefreshMarket();
            SetExpiration();
        }
    }

    private static int _nextId = 2000;
    public static int GenerateUniqueId()
    {
        return _nextId++;
    }
}



