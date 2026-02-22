[PacketHandler(MessageType.SupportGetAllTicketRequest)]
public static class GetAllTickets
{
    public static void Handle(Session session)
    {
        if (session.Account == null) return;
        var account = session.Account;
        var response = new SupportGetAllTicketResponsePacket
        {
            TicketBan = account.TicketBan
        };

        foreach (var ticket in account.Tickets)
        {
            var ticketInfo = new SupportGetAllTicketResponsePacket.TicketInfo
            {
                No = (byte)ticket.NO,
                Title = ticket.Title,
                IsClosed = ticket.IsClosed,
                ClosedReason = ticket.ClosedReason,
                ClosedAt = ticket.IsClosed ? (int)new DateTimeOffset(ticket.ClosedAt).ToUnixTimeSeconds() : 0
            };

            foreach (var msg in ticket.ticketMessages)
            {
                ticketInfo.Messages.Add(new SupportGetAllTicketResponsePacket.MessageInfo
                {
                    Name = msg.Name,
                    Content = msg.Message
                });
            }
            
            response.Tickets.Add(ticketInfo);
        }

        session.Send(response);

    }
}