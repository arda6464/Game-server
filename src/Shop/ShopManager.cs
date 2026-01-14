using System;
using System.Collections.Generic;
using System.Linq;

public static class ShopManager
{
    private static List<MarketItemData> BaseMarketOffer = new List<MarketItemData>();
    private static Dictionary<int, MarketOfferData> ActiveOffers = new Dictionary<int, MarketOfferData>();
    private static Dictionary<string, DateTime> PlayerPurchaseHistory = new Dictionary<string, DateTime>();
    
    public static DateTime ExpiresAt { get; private set; }
    public static TimeSpan RefreshInterval { get; private set; } = TimeSpan.FromHours(24);

    // Market başlatma
    public static void InitializeMarket()
    {
        if (BaseMarketOffer.Count == 0 || IsExpired())
        {
            GenerateBaseMarketItems();
            GenerateSpecialOffers();
            SetExpiration();
        }
    }

    // Temel market ürünlerini oluştur
    private static void GenerateBaseMarketItems()
    {
        BaseMarketOffer.Clear();

        // Örnek ürünler - gerçek oyununuza göre düzenleyin
        BaseMarketOffer.Add(new MarketItemData
        {
            itemId = GenerateUniqueId(),
            itemName = "Free Gems Pack",
            itemType = ItemType.Gems,
            basePrice = 2,
            Count = 100,
        });
        BaseMarketOffer.Add(new MarketItemData
        {
            itemId = GenerateUniqueId(),
            itemName = "Free Gems Pack",
            itemType = ItemType.Gems,
            basePrice = 1000,
            Count = 500,
        });
          BaseMarketOffer.Add(new MarketItemData
        {
            itemId = GenerateUniqueId(),
            itemName = "Senin icin <3",
            itemType = ItemType.Gems,
            basePrice = 100,
            Count = 100,
        });
          BaseMarketOffer.Add(new MarketItemData 
        { 
            itemId = GenerateUniqueId(), 
            itemName = "Developer Pack", 
            itemType = ItemType.Gems, 
            basePrice = 31,
            Count = 9999,
        });
        
       
        
      
    }

    // Özel teklifler oluştur
    private static void GenerateSpecialOffers()
    {
        ActiveOffers.Clear();
        
        // Günlük teklif
        var dailyOffer = new MarketOfferData
        {
            Title = "2025 yılı için Özel Teklif !",
            offerId = 2,
            itemType = ItemType.Coins, // Health Potion
            offerType = OfferType.DailyDeal,
           EndTime = new DateTime(2025,12,31,23,59,59), // yıl başına kadar geçerli
            basePrice = 50,
            Count = 500,
        };
        ActiveOffers.Add(dailyOffer.offerId, dailyOffer);
        
        // Flash sale (rastgele ürün)
        
            MarketOfferData flashOffer = new MarketOfferData
            {
                Title = "Flash Sale: 1000 Gems!",
                offerId = 1,
                itemType = ItemType.Gems,                           
                offerType = OfferType.FlashSale,
                EndTime = DateTime.UtcNow.AddHours(6),
                basePrice = 100,
                Count = 1000,
         
            };
            ActiveOffers.Add(flashOffer.offerId, flashOffer);
        
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
        InitializeMarket();
        return BaseMarketOffer;
    }
   public static List<MarketOfferData> GetOffers(string playerId = "")
    {
        InitializeMarket();
    return ActiveOffers.Values.ToList();
    }

    public static void RefreshMarket()  => InitializeMarket();



    public static void Update()
    {
        if (IsExpired())
        {
            RefreshMarket();
        }

        // Teklif sürelerini güncelle

    }
    private static int _nextId = 1000;

    public static int GenerateUniqueId()
    {
        return _nextId++;
    }
}




