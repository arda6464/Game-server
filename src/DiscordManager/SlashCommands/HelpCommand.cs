using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;


public static class helpCommand
{
    public static async Task HandleHelpSlashAsync(SocketSlashCommand command)
    {
        var embed = new EmbedBuilder()
            .WithTitle("🤖 Bot Komutları")
            .WithColor(Color.Blue)
            .AddField("🎯 Prefix Komutlar ( ! )",
                "`!ticket` - Yeni ticket açar\n" +
                "`!kapat` - Ticket'ı kapatır\n" +
                "`!yardım` - Yardım menüsü")
            .AddField("✨ Slash Komutlar ( / )",
                "`/ticket aç` - Yeni ticket açar\n" +
                "`/ticket kapat` - Ticket'ı kapatır\n" +
                "`/ticket liste` - Ticket'ları listeler\n" +
                "`/yardım` - Bu menüyü gösterir")
            .WithFooter("Her iki sistem de aktif!")
            .WithCurrentTimestamp()
            .Build();

        await command.RespondAsync(embed: embed, ephemeral: true);
    }
}