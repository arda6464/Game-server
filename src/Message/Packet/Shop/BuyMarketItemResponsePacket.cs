using System.Collections.Generic;

public class BuyMarketItemResponsePacket : IPacket
{
    public int Result { get; set; }       // PurchaseResult enum değeri
    public int NewGems { get; set; }
    public int NewCoins { get; set; }
    public int ItemId { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.BuyMarketItemResponse);



        
        buffer.WriteVarInt(Result);
        if ((PurchaseResult)Result == PurchaseResult.Success)
        {
            buffer.WriteVarInt(ItemId);
            buffer.WriteVarInt(NewGems);
            buffer.WriteVarInt(NewCoins);
        }

    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
