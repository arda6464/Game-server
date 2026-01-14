 [System.Serializable]
    public class MarketItemData
    {
        public int itemId;
        public string itemName;
        public ItemType itemType;
        public int basePrice;
    public int Count;
        

        //public string rarity;
       // public string description;
       // public string iconPath;
       // public int finalPrice;
      //  public bool hasDiscount;
       // public float discountPercent;
        //public bool isLimited;
        //public int stock;
    }

    [System.Serializable]
    public class MarketOfferData
    {
    public int offerId;
        public string Title;
       public ItemType itemType;
       // public float discountPercent;
        public OfferType offerType;
   
    
    public DateTime EndTime;
    public int Count;
    public int basePrice;
        
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
    BattlePass = 2,

}
