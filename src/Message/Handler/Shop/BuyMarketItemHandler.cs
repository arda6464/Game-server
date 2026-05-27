using System.Collections.Generic;

[PacketHandler(MessageType.BuyMarketItemRequest)]
public static class BuyMarketItemHandler
{
    public static void Handle(Session session, byte[] data)
    {
        int itemId;
        bool isOffer;

        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            buffer.WriteBytes(data, true);
            itemId = buffer.ReadVarInt();
            isOffer = buffer.ReadBool();
        }

        var account = AccountCache.Load(session.ID);
        if (account == null)
        {
            Logger.errorslog($"[BuyMarketItemHandler] Hesap bulunamadı: {session.ID}");
            return;
        }

        List<RewardItem> rewards;
        var result = ShopManager.TryBuyItem(account, itemId, out rewards, isOffer);

        var response = new BuyMarketItemResponsePacket
        {
            Result = (int)result,
            ItemId = itemId,
            NewGems = account.Gems,
            NewCoins = account.Coins
        };
        session.Send(response);

        // Başarılıysa güncel AccountData'yı ve ödülleri Gacha ile gönder (UI senkronizasyonu)
        if (result == PurchaseResult.Success)
        {
            if (rewards != null && rewards.Count > 0)
            {
                var gachaResponse = new GachaResponsePacket();
                foreach (var reward in rewards)
                {
                    gachaResponse.Drops.Add(new GachaSystem.GachaReward(reward));
                }
                session.Send(gachaResponse);
            }

            session.Send(new AccountDataPacket(account));
            Logger.genellog($"[BuyMarketItemHandler] {account.Username} ({account.ID}) → Item {itemId} satın alındı.");
        }
        else
        {
            Logger.genellog($"[BuyMarketItemHandler] {account.Username} ({account.ID}) → Item {itemId} başarısız: {result}");
        }
    }
}
