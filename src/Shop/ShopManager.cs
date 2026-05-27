using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

public static class ShopManager
{
    private static List<MarketItemData> BaseMarketOffer = new List<MarketItemData>();
    private static Dictionary<int, MarketOfferData> ActiveOffers = new Dictionary<int, MarketOfferData>();

    private static readonly string _itemsPath = "market_items.json";
    private static readonly string _offersPath = "market_offers.json";
    private static readonly object _lock = new object();

    // Satın alma rate-limit: oyuncu ID -> son satın alma zamanı
    private static readonly Dictionary<int, DateTime> _purchaseCooldowns = new Dictionary<int, DateTime>();
    private static readonly TimeSpan PurchaseCooldown = TimeSpan.FromSeconds(3);




    public static DateTime ExpiresAt { get; private set; }
    public static TimeSpan RefreshInterval { get; private set; } = TimeSpan.FromHours(24);

    static ShopManager()
    {
        Load();
    }

    // ─── Veri Yükleme / Kaydetme ─────────────────────────────────────────────

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
        BaseMarketOffer.Add(new MarketItemData { ItemId = GenerateUniqueId(), ItemName = "Küçük Elmas Paketi", ItemType = ItemType.Gems, PriceType = PriceType.Coins, BasePrice = 500, Count = 100 });
        BaseMarketOffer.Add(new MarketItemData { ItemId = GenerateUniqueId(), ItemName = "Büyük Elmas Paketi", ItemType = ItemType.Gems, PriceType = PriceType.Coins, BasePrice = 2000, Count = 600 });
        Save();
    }

    // ─── Ürün Yönetimi ───────────────────────────────────────────────────────

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

    // ─── Market Verileri ─────────────────────────────────────────────────────

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
            var expiredIds = ActiveOffers.Values
                .Where(o => o.EndTime < now)
                .Select(o => o.OfferId)
                .ToList();
            if (expiredIds.Count > 0)
            {
                foreach (var id in expiredIds) ActiveOffers.Remove(id);
                Save();
            }
        }

        return ActiveOffers.Values.ToList();
    }

    /// <summary>
    /// Oyuncuya özel kişisel teklifleri üretir (Hibrit sistem — DB'ye yazılmaz).
    /// FirstPurchase, LoyaltyReward ve PersonalDiscount tekliflerini koşullara göre döner.
    /// </summary>
    public static List<MarketOfferData> GeneratePersonalOffers(AccountManager.AccountData account)
    {
        var personalOffers = new List<MarketOfferData>();
        if (account == null) return personalOffers;

        var now = DateTime.UtcNow;

        // İlk Satın Alma Teklifi
        if (account.TotalPurchases == 0)
        {
            personalOffers.Add(new MarketOfferData
            {
                OfferId = GenerateUniqueId(),
                Title = "İlk Alım Fırsatı! 🎉",
                PriceType = PriceType.Coins,
                OfferType = OfferType.FirstPurchase,
                BasePrice = 1600,
                DiscountPercent = 20,
                EndTime = now.AddDays(3),
                IsPersonal = true,
                TargetAccountId = account.ID,
                Rewards = new List<RewardItem>
                {
                    new RewardItem { Type = ItemType.Gems,  Count = 600  },
                    new RewardItem { Type = ItemType.Coins, Count = 1000 }
                }
            });
        }

        // Sadakat Ödülü
        if (account.WinStreak >= 7)
        {
            personalOffers.Add(new MarketOfferData
            {
                OfferId = GenerateUniqueId(),
                Title = "Sadakat Ödülü! 🏆",
                PriceType = PriceType.Coins,
                OfferType = OfferType.LoyaltyReward,
                BasePrice = 0,
                DiscountPercent = 100,
                EndTime = now.AddDays(1),
                IsPersonal = true,
                TargetAccountId = account.ID,
                Rewards = new List<RewardItem>
                {
                    new RewardItem { Type = ItemType.XPBoost, Count = 3   },
                    new RewardItem { Type = ItemType.Coins,   Count = 500 }
                }
            });
        }

        // Geri Dönüş Teklifi
        if (account.TotalPurchases > 0 && account.LastPurchaseDate < now.AddDays(-3))
        {
            personalOffers.Add(new MarketOfferData
            {
                OfferId = GenerateUniqueId(),
                Title = "Seni Özledik! 💎",
                PriceType = PriceType.Coins,
                OfferType = OfferType.PersonalDiscount,
                BasePrice = 700,
                DiscountPercent = 30,
                EndTime = now.AddDays(2),
                IsPersonal = true,
                TargetAccountId = account.ID,
                Rewards = new List<RewardItem>
                {
                    new RewardItem { Type = ItemType.Gems,         Count = 300 },
                    new RewardItem { Type = ItemType.TrophyShield, Count = 3   }
                }
            });
        }

        return personalOffers;
    }

    // ─── Satın Alma ──────────────────────────────────────────────────────────

    /// <summary>
    /// Belirtilen ürünü oyuncuya satın aldırır. Tüm validasyon ve etki uygulama burada yapılır.
    /// </summary>
    public static PurchaseResult TryBuyItem(AccountManager.AccountData account, int itemId, out List<RewardItem> rewards, bool isOffer = false)
    {
        rewards = new List<RewardItem>();
        if (!DynamicConfigManager.Config.IsShopEnabled)
            return PurchaseResult.ShopDisabled;

        if (account == null)
            return PurchaseResult.ItemNotFound;

        // Rate-limit kontrolü
        lock (_purchaseCooldowns)
        {
            if (_purchaseCooldowns.TryGetValue(account.ID, out var lastPurchase))
            {
                if (DateTime.UtcNow - lastPurchase < PurchaseCooldown)
                    return PurchaseResult.RateLimited;
            }
        }

        if (isOffer)
        {
            // Önce kişisel teklif cache'inde ara
            var personalOffers = GeneratePersonalOffers(account);
            var personalOffer = personalOffers.FirstOrDefault(o => o.OfferId == itemId);
            if (personalOffer != null)
                return ProcessOfferPurchase(account, personalOffer, rewards);

            // Sonra global tekliflere bak
            lock (_lock)
            {
                if (!ActiveOffers.TryGetValue(itemId, out var offer))
                    return PurchaseResult.ItemNotFound;
                return ProcessOfferPurchase(account, offer, rewards);
            }
        }
        else
        {
            // Normal ürün
            MarketItemData item;
            lock (_lock)
            {
                item = BaseMarketOffer.FirstOrDefault(i => i.ItemId == itemId);
            }
            if (item == null) return PurchaseResult.ItemNotFound;

            int finalPrice = (item.IsDiscounted && item.DiscountedPrice > 0) ? item.DiscountedPrice : item.BasePrice;
            return ProcessItemPurchase(account, item.ItemType, item.PriceType, finalPrice, item.Count, item.ItemName, rewards);
        }
    }

    /// <summary>Çoklu reward içeren offer satın alımı</summary>
    private static PurchaseResult ProcessOfferPurchase(AccountManager.AccountData account, MarketOfferData offer, List<RewardItem> rewardsList)
    {
        int finalPrice = offer.BasePrice;
        if (offer.DiscountPercent > 0)
            finalPrice = (int)(finalPrice * (1 - offer.DiscountPercent / 100.0));

        lock (account.SyncLock)
        {
            // Para kontrolü
            if (finalPrice != 0)
            {
                var payCheck = CheckAndDeductPrice(account, offer.PriceType, finalPrice);
                if (payCheck != PurchaseResult.Success) return payCheck;

            }

            // Her reward'ı uygula
            foreach (var reward in offer.Rewards)
            {
                DeliveryManager.ApplyReward(account, reward);
                rewardsList.Add(reward);
            }

            account.TotalPurchases++;
            account.LastPurchaseDate = DateTime.UtcNow;
            lock (_purchaseCooldowns) { _purchaseCooldowns[account.ID] = DateTime.UtcNow; }

            var rewardSummary = string.Join(" + ", offer.Rewards.Select(r => $"{r.Count} {r.Type}"));
            Logger.genellog($"[ShopManager] Offer satın alma: {account.Username} ({account.ID}) → {offer.Title} | Ödüller: {rewardSummary} | Ödenen: {finalPrice} {offer.PriceType}");
        }
        return PurchaseResult.Success;
    }

    /// <summary>Tekil ürün satın alımı</summary>
    private static PurchaseResult ProcessItemPurchase(
        AccountManager.AccountData account,
        ItemType itemType,
        PriceType priceType,
        int price,
        int count,
        string itemName,
        List<RewardItem> rewardsList)
    {
        lock (account.SyncLock)
        {
            // Avatar için tekrar alım kontrolü
            if (itemType == ItemType.Avatar && account.OwnedItems.Contains(count))
                return PurchaseResult.AlreadyOwned;

            var payCheck = CheckAndDeductPrice(account, priceType, price);
            if (payCheck != PurchaseResult.Success) return payCheck;

            var reward = new RewardItem { Type = itemType, Count = count, DataId = count };
            DeliveryManager.ApplyReward(account, reward);
            rewardsList.Add(reward);

            account.TotalPurchases++;
            account.LastPurchaseDate = DateTime.UtcNow;
            lock (_purchaseCooldowns) { _purchaseCooldowns[account.ID] = DateTime.UtcNow; }

            Logger.genellog($"[ShopManager] Satın alma: {account.Username} ({account.ID}) → {itemName} | Ödenen: {price} {priceType}");
        }
        return PurchaseResult.Success;
    }

    /// <summary>Para kontrolü yapar ve yeterliyse düşer, değilse hata döner.</summary>
    private static PurchaseResult CheckAndDeductPrice(AccountManager.AccountData account, PriceType priceType, int price)
    {
        if (priceType == PriceType.Gems)
        {
            if (account.Gems < price) return PurchaseResult.NotEnoughGems;
            account.Gems -= price;
        }
        else if (priceType == PriceType.Coins)
        {
            if (account.Coins < price) return PurchaseResult.NotEnoughCoins;
            account.Coins -= price;
        }
        else if (priceType == PriceType.RealMoney)
        {
            // TODO: IAP receipt validation
            return PurchaseResult.RealMoneyNotSupported;
        }
        return PurchaseResult.Success;
    }


    // ─── Yardımcı Metodlar ───────────────────────────────────────────────────

    public static void InitializeMarket()
    {
        if (IsExpired()) SetExpiration();
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

    private static void SetExpiration()
    {
        ExpiresAt = DateTime.UtcNow.Add(RefreshInterval);
    }

    public static bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }

    private static int _nextId = 2000;
    public static int GenerateUniqueId() => _nextId++;
}
