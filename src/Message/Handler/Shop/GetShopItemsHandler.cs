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
                Id = item.ItemId,
                Name = item.ItemName,
                Type = (int)item.ItemType,
                Price = item.BasePrice,
                Count = item.Count
            });
        }
        
        foreach (var offerItem in offers)
        {
            response.Offers.Add(new GetAllMarketItemsResponsePacket.OfferItem
            {
                Title = offerItem.Title,
                Id = offerItem.OfferId,
                ItemType = (int)offerItem.ItemType,
                OfferType = (int)offerItem.OfferType,
                Price = offerItem.BasePrice,
                Count = offerItem.Count,
                EndTime = ((DateTimeOffset)offerItem.EndTime).ToUnixTimeSeconds()
            });
        }

        session.Send(response);
       
        Logger.genellog($"[ShopItemsHandler] {session.ID} kullanıcısına mağaza öğeleri gönderildi.");
       
    }
}