using System.Linq;
using System.Collections.Generic;
using System;

public class SupportTicketData
{
    public string? Username { get; set; }
    public int PlayerID { get; set; }
    public int ID { get; set; }
    public int NO { get; set; }
    public string? Title { get; set; }
    public List<TicketMessage> ticketMessages = new List<TicketMessage>();
    public bool IsClosed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ClosedAt { get; set; }
    public string? ClosedReason;
    public ulong channelid;
}

public class TicketMessage
{
    public string? Name;
    public string? Message;
    public DateTime time;
}

public static class TicketManager
{
    public static SupportTicketData GetTicketData(int playerid, int ticketid)
    {
        var account = AccountCache.Load(playerid);
        if (account == null) return null;

        SupportTicketData ticket = account.Tickets.FirstOrDefault(t => t.ID == ticketid);
        if (ticket == null)
        {
            return null;
        }
        return ticket;
    }

    public static SupportTicketData GetTicketDataByNo(int playerid, int ticketNo)
    {
        var account = AccountCache.Load(playerid);
        if (account == null) return null;

        SupportTicketData ticket = account.Tickets.FirstOrDefault(t => t.NO == ticketNo);
        if (ticket == null)
        {
            return null;
        }
        return ticket;
    }
}