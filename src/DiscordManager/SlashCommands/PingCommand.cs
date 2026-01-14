using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;

public static class PingCommand
{
public static async Task HandlePingSlashAsync(SocketSlashCommand command,BotManager bot)
    {
        var embed = new EmbedBuilder()
            .WithTitle("🏓 Pong!")
            .WithDescription($"Botun pingi: {bot.Client.Latency} ms")           
            .WithColor(Color.Green)
            .WithCurrentTimestamp()
            .Build();

        await command.RespondAsync(embed: embed, ephemeral: false);
    }
}