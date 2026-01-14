public static class GetAllTickets
{
    public static void Handle(Session session)
    {
        var account = AccountCache.Load(session.AccountId);
        using (ByteBuffer buffer = new ByteBuffer(2048))
        {
            buffer.WriteInt((int)MessageType.SupportGetAllTicketResponse);
            buffer.WriteBool(account.TicketBan);
            buffer.WriteByte((byte)account.Tickets.Count());
            foreach (var ticket in account.Tickets)
            {
                buffer.WriteByte((byte)ticket.NO);
                buffer.WriteString(ticket.Title ?? " ");
                buffer.WriteBool(ticket.IsClosed);
                if(ticket.IsClosed)
                {
                    buffer.WriteString(ticket.ClosedReason);
                    buffer.WriteInt((int)new DateTimeOffset(ticket.ClosedAt).ToUnixTimeSeconds());
                }
                buffer.WriteByte((byte)ticket.ticketMessages.Count);
                foreach (var msg in ticket.ticketMessages)
                {
                    buffer.WriteString(msg.Name);
                    buffer.WriteString(msg.Message);

                }
            }
            byte[] response = buffer.ToArray();
            session.Send(response)  ;
        }

    }
}