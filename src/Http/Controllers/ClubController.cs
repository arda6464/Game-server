using System.Linq;

[HttpController]
public class ClubController : BaseController
{
    [HttpRoute("GET", "/api/clubs")]
    public object ListClubs()
    {
        var clubs = ClubManager.Clubs.Values.ToList();
        return clubs.Select(c => new
        {
            id = c.ID,
            name = c.Name ?? "İsimsiz Kulüp",
            description = c.Description ?? "",
            avatarId = c.AvatarID,
            trophies = c.TotalTrophy,
            memberCount = c.Members.Count,
            maxMembers = c.MaxMembers,
            state = c.State.ToString(),
            region = c.Region ?? "Global",
            members = c.Members.Select(m => new
            {
                id = m.ID,
                name = m.AccountName,
                role = m.Role.ToString(),
                avatarId = m.AvatarID
            }).ToList()
        }).ToList();
    }

    [HttpRoute("POST", "/api/clubs/update")]
    public object UpdateClub(SimpleHttpContext ctx)
    {
        var data = ReadFormData();
        if (data == null || !data.ContainsKey("id"))
            return Fail("Kulüp ID gerekli.");

        if (!int.TryParse(data["id"], out int clubId))
            return Fail("Geçersiz Kulüp ID.");

        string name = data.ContainsKey("name") ? data["name"] : "";
        string desc = data.ContainsKey("description") ? data["description"] : "";
        int avatarId = data.ContainsKey("avatarId") ? int.Parse(data["avatarId"]) : 0;
        int state = data.ContainsKey("state") ? int.Parse(data["state"]) : 0;
        string region = data.ContainsKey("region") ? data["region"] : "Global";

        var club = ClubManager.LoadClub(clubId);
        if (club == null)
            return Fail("Kulüp bulunamadı.");

        bool success = club.ChangeClubSettings(0, name, desc, avatarId, state, region);
        if (success)
            Audit("Kulüp Güncellendi", name, $"Kulüp ID: {clubId}");

        return success ? Ok("Kulüp ayarları kaydedildi.") : Fail("Hata oluştu.");
    }

    [HttpRoute("POST", "/api/clubs/delete")]
    public object DeleteClub(SimpleHttpContext ctx)
    {
        var data = ReadFormData();
        if (data == null || !data.ContainsKey("id"))
            return Fail("Kulüp ID gerekli.");

        int clubId = int.Parse(data["id"]);
        var club = ClubManager.LoadClub(clubId);
        bool success = ClubManager.DeleteClub(clubId);

        if (success)
            Audit("Kulüp Silindi", club?.Name ?? clubId.ToString(), "Kulüp başarıyla silindi.");

        return success ? Ok("Kulüp silindi.") : Fail("Kulüp silinemedi.");
    }

    [HttpRoute("POST", "/api/clubs/kick")]
    public object KickMember(SimpleHttpContext ctx)
    {
        var data = ReadFormData();
        if (data == null || !data.ContainsKey("clubId") || !data.ContainsKey("memberId"))
            return Fail("clubId ve memberId gerekli.");

        int clubId = int.Parse(data["clubId"]);
        int memberId = int.Parse(data["memberId"]);

        var club = ClubManager.LoadClub(clubId);
        bool success = club != null && club.RemoveMember(memberId);

        if (success)
        {
            var memberAccount = AccountCache.Load(memberId);
            if (memberAccount != null)
            {
                memberAccount.Clubid = 0;
                memberAccount.ClubName = null;
                memberAccount.clubRole = ClubRole.None;
                AccountManager.SaveAccounts();

                var memberSession = SessionManager.GetSession(memberId);
                if (memberSession != null && memberSession.IsConnected)
                {
                    memberSession.Send(new LeaveClubResponsePacket { Kicked = true });
                    memberSession.Logic?.SendUpdate();
                }
            }
            Audit("Üye Kovuldu", memberId.ToString(), $"Kulüp ID: {clubId}");
        }

        return success ? Ok("Oyuncu kulüpten atıldı.") : Fail("Oyuncu atılamadı.");
    }
}
