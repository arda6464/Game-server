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
        buffer.WriteShort((short)MessageType.GetAllMarketItemsResponse);
        buffer.WriteInt(Items.Count);
        foreach (var item in Items)
        {
            buffer.WriteInt(item.Id);
            buffer.WriteString(item.Name);
            buffer.WriteInt(item.Type);
            buffer.WriteInt(item.Price);
            buffer.WriteInt(item.Count);
        }

        bool hasOffers = Offers.Count > 0;
        buffer.WriteBool(hasOffers);

        if (hasOffers)
        {
            buffer.WriteInt(Offers.Count);
            foreach (var offer in Offers)
            {
                buffer.WriteString(offer.Title);
                buffer.WriteInt(offer.Id);
                buffer.WriteInt(offer.ItemType);
                buffer.WriteInt(offer.OfferType);
                buffer.WriteInt(offer.Price);
                buffer.WriteInt(offer.Count);
                buffer.WriteLong(offer.EndTime);
            }
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
