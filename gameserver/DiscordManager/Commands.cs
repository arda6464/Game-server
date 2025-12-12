using Discord.WebSocket;

public class Commands
{
    private BotManager bot;

    public Commands(BotManager manager)
    {
        bot = manager;
    }

    public void Process(SocketMessage msg)
    {
        string c = msg.Content;

        if (c.StartsWith("!ticket"))
        {
            bot.TicketSystem.CreateTicket(msg.Author.Id.ToString(), new TicketData
            {

            });
            msg.Channel.SendMessageAsync("Ticket açıldı!").GetAwaiter().GetResult();
        }

        if (c.StartsWith("!kapat"))
        {
            bot.TicketSystem.CloseTicket(msg.Channel.Id);
        }
    }
}
