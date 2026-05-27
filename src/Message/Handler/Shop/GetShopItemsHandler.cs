[PacketHandler(MessageType.GetAllMarketItemsRequest)]
public static class ShopItemsHandler
{
    public static void Handle(Session session)
    {
        var account = AccountCache.Load(session.ID);

        var baseshopItems = ShopManager.GetMarketItems();
        var globalOffers  = ShopManager.GetOffers();

        // Kişisel teklifleri üret (hibrit sistem — DB'ye yazılmaz) 
        var personalOffers = (account != null)
            ? ShopManager.GeneratePersonalOffers(account)
            : new System.Collections.Generic.List<MarketOfferData>();

        var response = new GetAllMarketItemsResponsePacket();

        // ─── Ürünler ──────────────────────────────────────────────────────────
        foreach (var item in baseshopItems)
        {
            response.Items.Add(new GetAllMarketItemsResponsePacket.ShopItem
            {
                Id             = item.ItemId,
                Name           = item.ItemName,
                Type           = (int)item.ItemType,
                Price          = item.BasePrice,
                Count          = item.Count,
                IsDiscounted   = item.IsDiscounted,
                DiscountedPrice = item.DiscountedPrice
            });
        }

        // ─── Global Teklifler ─────────────────────────────────────────────────
        foreach (var offer in globalOffers)
        {
            var offerItem = BuildOfferItem(offer, offer.BasePrice);
            response.Offers.Add(offerItem);
        }

        // ─── Kişisel Teklifler ────────────────────────────────────────────────
        foreach (var personal in personalOffers)
        {
            int finalPrice = personal.BasePrice;
            if (personal.DiscountPercent > 0)
                finalPrice = (int)(personal.BasePrice * (1 - personal.DiscountPercent / 100.0));

            var offerItem = BuildOfferItem(personal, finalPrice);
            response.Offers.Add(offerItem);
        }

        session.Send(response);

        Logger.genellog(
            $"[ShopItemsHandler] {session.ID} → {baseshopItems.Count} ürün, " +
            $"{globalOffers.Count} global + {personalOffers.Count} kişisel teklif gönderildi.");
    }

    private static GetAllMarketItemsResponsePacket.OfferItem BuildOfferItem(MarketOfferData offer, int displayPrice)
    {
        var offerItem = new GetAllMarketItemsResponsePacket.OfferItem
        {
            Title     = offer.Title,
            Id        = offer.OfferId,
            OfferType = (int)offer.OfferType,
            Price     = displayPrice,
            EndTime   = ((DateTimeOffset)offer.EndTime).ToUnixTimeSeconds()
        };

        foreach (var reward in offer.Rewards)
        {
            offerItem.Rewards.Add(new GetAllMarketItemsResponsePacket.OfferReward
            {
                Type  = (int)reward.Type,
                Count = reward.Count
            });
        }

        return offerItem;
    }
}