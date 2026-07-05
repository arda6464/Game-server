[HttpController]
public class GachaController : BaseController
{
    [HttpRoute("GET", "/api/gacha/all")]
    public object GetAll() => GachaSystem.GachaManager.GetAllBoxes();

    [HttpRoute("POST", "/api/gacha/add")]
    public object Add()
    {
        var box = ReadJsonBody<GachaSystem.GachaBox>();
        if (box == null) return Fail("Geçersiz sandık verisi.");
        GachaSystem.GachaManager.AddOrUpdateBox(box);
        Audit("Gacha Sandık Ekleme/Güncelleme", box.Name, $"{box.Drops.Count} adet drop tanımlandı.");
        return Ok("Sandık başarıyla kaydedildi.");
    }

    [HttpRoute("POST", "/api/gacha/remove")]
    public object Remove()
    {
        var data = ReadJsonBody<Dictionary<string, int>>();
        if (data == null || !data.ContainsKey("id")) return Fail("ID gerekli.");
        GachaSystem.GachaManager.RemoveBox(data["id"]);
        Audit("Gacha Sandık Silme", $"ID: {data["id"]}", "Sandık sistemden kaldırıldı.");
        return Ok("Sandık silindi.");
    }
}
