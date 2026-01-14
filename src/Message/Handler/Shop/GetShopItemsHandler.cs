public static class ShopItemsHandler
{
    public static void Handle(Session session)
    {


        var baseshopItems = ShopManager.GetMarketItems();
        var offers = ShopManager.GetOffers();
        bool offer = offers.Count > 0; // offer packet daha sonra değiştirilcek 
        ByteBuffer buffer = new ByteBuffer();

        buffer.WriteInt((int)MessageType.GetAllMarketItemsResponse);
        buffer.WriteInt(baseshopItems.Count);
        foreach (var item in baseshopItems)
        {
            buffer.WriteInt(item.itemId);
            buffer.WriteString(item.itemName);
            buffer.WriteInt((int)item.itemType);
            buffer.WriteInt(item.basePrice);
            buffer.WriteInt(item.Count);
        }
        buffer.WriteBool(offer);
        Console.WriteLine("Offer durumu: " + offer);
        if (offer)
        {
            buffer.WriteInt(offers.Count);
            foreach (var offerItem in offers)
            {
                buffer.WriteString(offerItem.Title);
                buffer.WriteInt(offerItem.offerId);
                buffer.WriteInt((int)offerItem.itemType);
                buffer.WriteInt((int)offerItem.offerType);
                buffer.WriteInt(offerItem.basePrice);
                buffer.WriteInt(offerItem.Count);
                long unixTime = ((DateTimeOffset)offerItem.EndTime).ToUnixTimeSeconds();
                buffer.WriteLong(unixTime);

            }
        }
        byte[] response = buffer.ToArray();
        buffer.Dispose();
        session.Send(response);
       
        Logger.genellog($"[ShopItemsHandler] {session.AccountId} kullanıcısına mağaza öğeleri gönderildi.");
       
    }
}