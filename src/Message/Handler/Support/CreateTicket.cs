using System;
using System.Linq;
using System.Collections.Generic;

[PacketHandler(MessageType.SupporCreateTicketRequest)]
public static class CreateTicket
{
    public static void Handle(Session session,byte[] data)
    {
        byte reasontype;
        using (ByteBuffer read = new ByteBuffer())
        {
            read.WriteBytes(data);
            
            var request = new CreateTicketRequestPacket();
            request.Deserialize(read);
            
            reasontype = request.ReasonType;
        }

        if (session.Account == null) return;
        var account = session.Account;
        if (account.TicketBan) return;
        if (account.Tickets.Count > 10) return;

        if (BotManager.istance.TicketSystem != null)
        {
            var existingTicketIds = account.Tickets.Select(t => t.NO).ToList();

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

            SupportTicketData support = new SupportTicketData
            {
                PlayerID = session.ID,
                Username = account.Username,
                Title = Title,
                NO = newTicketId,
                ID = TicketStorage.MaxTicketID++,
                CreatedAt = DateTime.Now,
                ticketMessages = new List<TicketMessage>(),
            };

            account.Tickets.Add(support);
            BotManager.istance.TicketSystem.CreateTicket(session.ID, support);
        }
        else Console.WriteLine("bota ulaşılmıyor...");
    }
}
