[HttpController]
public class MarketController : BaseController
{
    [HttpRoute("GET", "/api/market/analytics")]
    public object Analytics()
    {
        var allAccounts = AccountCache.GetCachedAccounts().Values.ToList();
        var totalGems = allAccounts.Sum(a => (long)a.Gems);
        var totalCoins = allAccounts.Sum(a => (long)a.Coins);
        var topRich = allAccounts.OrderByDescending(a => a.Gems).Take(10).Select(a => new
        {
            username = a.Username,
            gems = a.Gems,
            coins = a.Coins
        }).ToList();

        return new
        {
            totalGems,
            totalCoins,
            playerCount = allAccounts.Count,
            topRich
        };
    }

    [HttpRoute("GET", "/api/market/all")]
    public object All([FromQuery] string? id)
    {
        if (!string.IsNullOrEmpty(id) && int.TryParse(id, out int playerId))
        {
            var account = AccountCache.Load(playerId);
            if (account == null) return Fail("Oyuncu bulunamadı.");

            var items = ShopManager.GetMarketItems(playerId.ToString());
            var globalOffers = ShopManager.GetOffers(playerId.ToString());
            var offers = globalOffers.Where(o => !o.IsPersonal || o.TargetAccountId == playerId).ToList();
            var personal = ShopManager.GeneratePersonalOffers(account);
            offers.AddRange(personal);

            return new
            {
                success = true,
                items,
                offers,
                player = new
                {
                    id = account.ID,
                    username = account.Username,
                    gems = account.Gems,
                    coins = account.Coins,
                    trophies = account.Trophy
                }
            };
        }

        return new
        {
            success = true,
            items = ShopManager.GetMarketItems(),
            offers = ShopManager.GetOffers()
        };
    }

    [HttpRoute("POST", "/api/market/item/add")]
    public object AddItem([FromBody] MarketItemData item)
    {
        if (item == null) return Fail("Geçersiz ürün verisi.");
        ShopManager.AddItem(item);
        Audit("Market Ürün Ekleme", item.ItemName, $"{item.Count} adet, {item.BasePrice} Fiyat");
        return Ok();
    }

    [HttpRoute("POST", "/api/market/item/remove")]
    public object RemoveItem(SimpleHttpContext ctx)
    {
        Context = ctx;
        var data = ReadJsonBody<Dictionary<string, string>>();
        if (data == null || !data.ContainsKey("id")) return Fail("ID gerekli.");
        int id = int.Parse(data["id"]);
        ShopManager.RemoveItem(id);
        Audit("Market Ürün Silme", id.ToString(), "Ürün marketten kaldırıldı.");
        return Ok();
    }

    [HttpRoute("POST", "/api/market/offer/add")]
    public object AddOffer([FromBody] MarketOfferData offer)
    {
        if (offer == null) return Fail("Geçersiz teklif verisi.");
        ShopManager.AddOffer(offer);
        Audit("Market Teklif Ekleme", offer.Title, $"{offer.Rewards?.FirstOrDefault()?.Count ?? 0} adet, {offer.BasePrice} Fiyat");
        return Ok();
    }

    [HttpRoute("POST", "/api/market/offer/remove")]
    public object RemoveOffer(SimpleHttpContext ctx)
    {
        Context = ctx;
        var data = ReadJsonBody<Dictionary<string, string>>();
        if (data == null || !data.ContainsKey("id")) return Fail("ID gerekli.");
        int id = int.Parse(data["id"]);
        ShopManager.RemoveOffer(id);
        Audit("Market Teklif Silme", id.ToString(), "Teklif marketten kaldırıldı.");
        return Ok();
    }
}
