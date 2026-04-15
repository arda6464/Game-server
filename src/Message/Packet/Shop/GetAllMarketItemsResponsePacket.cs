using System.Collections.Generic;

public class GetAllMarketItemsResponsePacket : IPacket
{
    public class ShopItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public int Price { get; set; }
        public int Count { get; set; }
    }

    public class OfferItem
    {
        public string Title { get; set; }
        public int Id { get; set; }
        public int ItemType { get; set; }
        public int OfferType { get; set; }
        public int Price { get; set; }
        public int Count { get; set; }
        public long EndTime { get; set; }
    }

    public List<ShopItem> Items { get; set; } = new List<ShopItem>();
    public List<OfferItem> Offers { get; set; } = new List<OfferItem>();

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.GetAllMarketItemsResponse);
        buffer.WriteVarInt(Items.Count);
        foreach (var item in Items)
        {
            buffer.WriteVarInt(item.Id);
            buffer.WriteVarString(item.Name);
            buffer.WriteVarInt(item.Type);
            buffer.WriteVarInt(item.Price);
            buffer.WriteVarInt(item.Count);
        }

        bool hasOffers = Offers.Count > 0;
        buffer.WriteBool(hasOffers);

        if (hasOffers)
        {
            buffer.WriteVarInt(Offers.Count);
            foreach (var offer in Offers)
            {
                buffer.WriteVarString(offer.Title);
                buffer.WriteVarInt(offer.Id);
                buffer.WriteVarInt(offer.ItemType);
                buffer.WriteVarInt(offer.OfferType);
                buffer.WriteVarInt(offer.Price);
                buffer.WriteVarInt(offer.Count);
                buffer.WriteVarLong(offer.EndTime);
            }
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
