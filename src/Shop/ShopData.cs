using System.Collections.Generic;

    [System.Serializable]
    public class MarketItemData
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public ItemType ItemType { get; set; }
        public PriceType PriceType { get; set; }
        public int BasePrice { get; set; }
        public int Count { get; set; }
        public bool IsDiscounted { get; set; } = false;
        public int DiscountedPrice { get; set; } = 0;   // 0 = indirim yok
        public string IconKey { get; set; } = "";        // İstemci tarafı ikon adı
        public bool IsLimited { get; set; } = false;     // Sınırlı ürün mü?
    }

    /// <summary>
    /// Bir ödül öğesi. Offer içinde birden fazla reward tanımlanabilir.
    /// Örnek: 200 Gems + 2000 Coins + Avatar kilidi
    /// </summary>
    [System.Serializable]
    public class RewardItem
    {
        public ItemType Type { get; set; }   // Ne verileceği
        public int Count { get; set; }       // Miktar (Gems/Coins miktarı vb.)
        public int DataId { get; set; }      // Ek veri ID'si (Avatar ID, Skin ID, Character ID vb.)
    }

    [System.Serializable]
    public class MarketOfferData
    {
        public int OfferId { get; set; }
        public string Title { get; set; }
        public PriceType PriceType { get; set; }
        public OfferType OfferType { get; set; }
        public DateTime EndTime { get; set; }
        public int BasePrice { get; set; }
        public int DiscountPercent { get; set; } = 0;  // % indirim oranı (0-100)
        public bool IsPersonal { get; set; } = false;  // Kişiye özel teklif mi?
        public int TargetAccountId { get; set; } = -1; // -1 = herkese açık
        public List<RewardItem> Rewards { get; set; } = new List<RewardItem>(); // Çoklu ödüller
    }

    public enum PriceType
    {
        Gems = 0,
        Coins = 1,
        RealMoney = 2 // TL/USD etc.
    }

    public enum OfferType
    {
        DailyDeal = 0,
        PersonalDiscount = 1,
        FirstPurchase = 2,
        LoyaltyReward = 3,
        FlashSale = 4,
        Seasonal = 5
    }

    public enum ItemType
    {
        Gems = 0,           // Elmas paketi
        Coins = 1,          // Altın paketi
        BattlePass = 2,     // Battle Pass
        Avatar = 3,         // Avatar/Skin kilidi (DataId = ID)
        XPBoost = 4,        // Geçici XP çarpanı (Count = gün sayısı)
        TrophyShield = 5,   // Kupa kaybetme koruması (Count = maç sayısı)
        StarterBundle = 6,  // Combo paket
        Character = 7,      // Karakter kilidi (DataId = ID)
        PowerPoints = 8,    // Karakter puanı (Count = Miktar, DataId = KarakterID)
        Emote = 9,          // İfade/Pin (DataId = ID)
        GachaBox = 10,      // Sandık (DataId = BoxID)
        Skin = 11,          // Skin kilidi (DataId = ID)
    }

    public enum PurchaseResult
    {
        Success               = 0,
        ItemNotFound          = 1,
        NotEnoughGems         = 2,
        NotEnoughCoins        = 3,
        ShopDisabled          = 4,
        RealMoneyNotSupported = 5,
        RateLimited           = 6,
        AlreadyOwned          = 7,
        OfferNotEligible      = 8, // Teklif koşulları sağlanmıyor
    }
