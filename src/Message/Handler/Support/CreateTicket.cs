using System.Security.Cryptography;

public static class CreateTicket
{
    public static void Handle(Session session,byte[] data)
    {

        byte reasontype;
        using (ByteBuffer read = new ByteBuffer())

        {
            read.WriteBytes(data);
            read.ReadInt();
            reasontype = read.ReadByte();
        }
        var acccount = AccountCache.Load(session.AccountId);
        if (acccount.TicketBan) return;
        if (acccount.Tickets.Count > 10) return;
        if (BotManager.istance.TicketSystem != null)
        {
            var existingTicketIds = acccount.Tickets.Select(t => t.NO).ToList();

            string? Title;
            int newTicketId = 1;
            while (existingTicketIds.Contains(newTicketId))
            {
                newTicketId++;
            }
            switch (reasontype)
            {
                case 1:
                    Title = $"Teknik destek - #{newTicketId}";
                    break;
                case 2:
                    Title = $"Satın Alım - #{newTicketId}";
                    break;
                case 3:
                    Title = $"Genel - #{newTicketId}";
                    break;
                default:
                    Title = $"Bilinmiyor - #{newTicketId}";
                    break;
            }
            Random random = new Random();
            SupportTicketData support = new SupportTicketData
            {
                AccountId = session.AccountId,
                Username = acccount.Username,
                Title = Title,
                NO = newTicketId,
                ID = TicketStorage.MaxTicketID++,
                CreatedAt = DateTime.Now,
                ticketMessages = new List<TicketMessage>(),

            };
            acccount.Tickets.Add(support);
            BotManager.istance.TicketSystem.CreateTicket(session.AccountId,support);
        }
        else Console.WriteLine("bota ulaşılmıyor...");
    }
}