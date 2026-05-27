using System.Collections.Generic;

[PacketHandler(MessageType.GetAllMarketItemsRequest)]
public class GetAllMarketItemsRequestPacket : IPacket
{
    public void Serialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(ByteBuffer buffer)
    {
        // Empty
    }
}


public class GetAllMarketItemsResponsePacket : IPacket
{
    public class ShopItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public int Price { get; set; }
        public int Count { get; set; }
        public bool IsDiscounted { get; set; }
        public int DiscountedPrice { get; set; }
    }

    public class OfferReward
    {
        public int Type { get; set; }   // ItemType enum değeri
        public int Count { get; set; }  // Miktar
    }

    public class OfferItem
    {
        public string Title { get; set; }
        public int Id { get; set; }
        public int OfferType { get; set; }
        public int Price { get; set; }
        public long EndTime { get; set; }
        public List<OfferReward> Rewards { get; set; } = new List<OfferReward>();
    }

    public List<ShopItem> Items { get; set; } = new List<ShopItem>();
    public List<OfferItem> Offers { get; set; } = new List<OfferItem>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.GetAllMarketItemsResponse);

        // ─── Ürünler ───────────────────────────────────────────────
        buffer.WriteVarInt(Items.Count);
        foreach (var item in Items)
        {
            buffer.WriteVarInt(item.Id);
            buffer.WriteVarString(item.Name);
            buffer.WriteVarInt(item.Type);
            buffer.WriteVarInt(item.Price);
            buffer.WriteVarInt(item.Count);
            buffer.WriteBool(item.IsDiscounted);
            if (item.IsDiscounted)
                buffer.WriteVarInt(item.DiscountedPrice);
        }

        // ─── Teklifler (çoklu reward destekli) ─────────────────────
        bool hasOffers = Offers.Count > 0;
        buffer.WriteBool(hasOffers);

        if (hasOffers)
        {
            buffer.WriteVarInt(Offers.Count);
            foreach (var offer in Offers)
            {
                buffer.WriteVarString(offer.Title);
                buffer.WriteVarInt(offer.Id);
                buffer.WriteVarInt(offer.OfferType);
                buffer.WriteVarInt(offer.Price);
                buffer.WriteVarLong(offer.EndTime);

                // Reward listesi
                buffer.WriteVarInt(offer.Rewards.Count);
                foreach (var reward in offer.Rewards)
                {
                    buffer.WriteVarInt(reward.Type);
                    buffer.WriteVarInt(reward.Count);
                }
            }
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
