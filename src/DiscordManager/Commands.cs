using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

public class PrefixCommands
{
    private BotManager bot;

    public PrefixCommands(BotManager manager)
    {
        bot = manager;
    }

    public async Task ProcessAsync(SocketMessage msg)
    {
        string content = msg.Content.ToLower();
        var channel = msg.Channel as SocketTextChannel;
        
        // Komutları ayır: !komut arg1 arg2
        var parts = content.Split(' ');
        var command = parts[0];
        var args = parts.Length > 1 ? parts[1..] : new string[0];

        switch (command)
        {
            case "!ticket":
                await HandleTicketCommandAsync(msg, args);
                break;
                
            case "!kapat":
                await HandleCloseCommandAsync(msg, args);
                break;
                
            case "!yardım":
                await msg.Channel.SendMessageAsync(
                    "**Prefix Komutlar:**\n" +
                    "`!ticket` - Yeni ticket açar\n" +
                    "`!kapat` - Ticket'ı kapatır\n" +
                    "`!yardım` - Bu mesajı gösterir\n\n" +
                    "**Slash Komutlar:**\n" +
                    "`/ticket` - Ticket sistemi\n" +
                    "`/yardım` - Yardım menüsü");
                break;
                
            default:
                await msg.Channel.SendMessageAsync($"Bilinmeyen komut: {command}. `!yardım` yazın.");
                break;
        }
    }

    private async Task HandleTicketCommandAsync(SocketMessage msg, string[] args)
    {
       /* bot.TicketSystem.CreateTicket(msg.Author.Id, new SupportTicketData
        {
            // Ticket verileri
        });*/
        
        var embed = new EmbedBuilder()
            .WithTitle("✅ Ticket Açıldı")
            .WithDescription("Ticket'ınız başarıyla oluşturuldu!")
            .WithColor(Color.Green)
            .WithFooter(f => f.Text = $"Kullanıcı: {msg.Author.Username}")
            .WithCurrentTimestamp()
            .Build();
            
        await msg.Channel.SendMessageAsync(embed: embed);
    }

    private async Task HandleCloseCommandAsync(SocketMessage msg, string[] args)
    {
        bot.TicketSystem.CloseTicketAsync(msg.Channel.Id,"test");
        
        var embed = new EmbedBuilder()
            .WithTitle("🔒 Ticket Kapatıldı")
            .WithDescription("Ticket başarıyla kapatıldı!")
            .WithColor(Color.Orange)
            .WithCurrentTimestamp()
            .Build();
            
        await msg.Channel.SendMessageAsync(embed: embed);
    }
}