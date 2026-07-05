[HttpController]
public class UpdateController : BaseController
{
    [HttpRoute("GET", "/api/updates/all")]
    public object All()
    {
        return UpdateNotesManager.GetAll();
    }

    [HttpRoute("POST", "/api/updates/save")]
    public object Save([FromBody] GameUpdateNoteData note)
    {
        if (note == null) return Fail("Geçersiz güncelleme verisi.");
        var saved = UpdateNotesManager.Save(note);
        Audit("Güncelleme Notu Kaydet", saved.Version, saved.Title);
        return new { success = true, message = "Güncelleme kaydedildi.", note = saved };
    }

    [HttpRoute("POST", "/api/updates/delete")]
    public object Delete([FromBody] Dictionary<string, int> data)
    {
        if (data == null || !data.ContainsKey("id")) return Fail("Geçersiz istek.");
        bool ok = UpdateNotesManager.Delete(data["id"]);
        if (ok) Audit("Güncelleme Notu Sil", $"ID: {data["id"]}", "Not silindi.");
        return ok ? Ok("Güncelleme silindi.") : Fail("Kayıt bulunamadı.");
    }

    [HttpRoute("POST", "/api/updates/publish")]
    public object Publish([FromBody] Dictionary<string, object> data)
    {
        if (data == null || !data.ContainsKey("id") || !data.ContainsKey("published")) return Fail("Geçersiz istek.");
        int id = Convert.ToInt32(data["id"]);
        bool published = Convert.ToBoolean(data["published"]);
        bool ok = UpdateNotesManager.SetPublishState(id, published);
        if (ok) Audit(published ? "Güncelleme Yayınla" : "Güncelleme Yayından Kaldır", $"ID: {id}", "");
        return ok
            ? Ok(published ? "Yayınlandı." : "Yayından kaldırıldı.")
            : Fail("Kayıt bulunamadı.");
    }

    [HttpRoute("POST", "/api/updates/reorder")]
    public object Reorder([FromBody] Dictionary<string, List<int>> data)
    {
        if (data == null || !data.ContainsKey("ids")) return Fail("Geçersiz istek.");
        UpdateNotesManager.Reorder(data["ids"]);
        return Ok("Sıralama güncellendi.");
    }
}
