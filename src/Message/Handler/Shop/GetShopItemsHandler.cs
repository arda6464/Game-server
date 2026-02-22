[PacketHandler(MessageType.GetAllMarketItemsRequest)]
public static class ShopItemsHandler
{
    public static void Handle(Session session)
    {


        var baseshopItems = ShopManager.GetMarketItems();
        var offers = ShopManager.GetOffers();
        bool offer = offers.Count > 0; // offer packet daha sonra değiştirilcek 
        var response = new GetAllMarketItemsResponsePacket();
        
        foreach (var item in baseshopItems)
        {
            response.Items.Add(new GetAllMarketItemsResponsePacket.ShopItem
            {
                Id = item.itemId,
                Name = item.itemName,
                Type = (int)item.itemType,
                Price = item.basePrice,
                Count = item.Count
            });
        }
        
        foreach (var offerItem in offers)
        {
            response.Offers.Add(new GetAllMarketItemsResponsePacket.OfferItem
            {
                Title = offerItem.Title,
                Id = offerItem.offerId,
                ItemType = (int)offerItem.itemType,
                OfferType = (int)offerItem.offerType,
                Price = offerItem.basePrice,
                Count = offerItem.Count,
                EndTime = ((DateTimeOffset)offerItem.EndTime).ToUnixTimeSeconds()
            });
        }

        session.Send(response);
       
        Logger.genellog($"[ShopItemsHandler] {session.AccountId} kullanıcısına mağaza öğeleri gönderildi.");
       
    }
}