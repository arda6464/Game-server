using System.Text;
using Newtonsoft.Json;

[HttpController]
public class SupportController : BaseController
{
    [HttpRoute("GET", "/api/support/tickets")]
    public object GetTickets()
    {
        var activeTickets = BotManager.istance.TicketSystem.GetActiveTickets();
        return activeTickets.Select(t => new
        {
            id = t.ID,
            no = t.NO,
            username = t.Username,
            playerId = t.PlayerID,
            title = t.Title,
            isClosed = t.IsClosed,
            createdAt = t.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
            lastMessage = t.ticketMessages.LastOrDefault()?.Message ?? "Mesaj yok",
            messages = t.ticketMessages.Select(m => new
            {
                name = m.Name,
                message = m.Message,
                time = m.time.ToString("HH:mm")
            }).ToList()
        }).ToList();
    }

    [HttpRoute("POST", "/api/support/reply")]
    public object Reply([FromBody] Dictionary<string, string> data)
    {
        if (data == null || !data.ContainsKey("ticketId") || !data.ContainsKey("message"))
            return Fail("ticketId ve message gerekli.");

        if (!int.TryParse(data["ticketId"], out int ticketId))
            return Fail("Geçersiz ticket ID.");

        string msg = data["message"];
        var ticket = BotManager.istance.TicketSystem.GetTicketDataByTicketID(ticketId);
        if (ticket == null)
            return Fail("Bilet bulunamadı.");

        BotManager.istance.TicketSystem.SendTicketMessage(ticket.PlayerID, $"(Admin) {msg}", ticketId);
        ticket.ticketMessages.Add(new TicketMessage { Name = "Admin", Message = msg, time = DateTime.Now });
        return Ok("Cevap gönderildi.");
    }

    [HttpRoute("POST", "/api/support/close")]
    public object Close([FromBody] Dictionary<string, string> data)
    {
        if (data == null || !data.ContainsKey("ticketId"))
            return Fail("ticketId gerekli.");

        if (!int.TryParse(data["ticketId"], out int ticketId))
            return Fail("Geçersiz ticket ID.");

        string reason = data.ContainsKey("reason") ? data["reason"] : "Admin tarafından kapatıldı.";
        var ticket = BotManager.istance.TicketSystem.GetTicketDataByTicketID(ticketId);
        if (ticket == null)
            return Fail("Bilet bulunamadı.");

        BotManager.istance.TicketSystem.CloseTicketAsync(ticket.channelid, reason, ticketId);
        return Ok("Bilet kapatıldı.");
    }
}
