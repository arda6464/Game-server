    [System.Serializable]
    public class MarketItemData
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public ItemType ItemType { get; set; }
        public PriceType PriceType { get; set; }
        public int BasePrice { get; set; }
        public int Count { get; set; }
    }

    [System.Serializable]
    public class MarketOfferData
    {
        public int OfferId { get; set; }
        public string Title { get; set; }
        public ItemType ItemType { get; set; }
        public PriceType PriceType { get; set; }
        public OfferType OfferType { get; set; }
        public DateTime EndTime { get; set; }
        public int Count { get; set; }
        public int BasePrice { get; set; }
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
        Gems = 0,
        Coins = 1,
        BattlePass = 2
    }
