using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;

public static class ServerBanStatsCommand
{
    public static async Task HandleServerBanStatsSlashAsync(SocketSlashCommand command)
     {
        // Ban istatistiklerini gösterme işlemi
      //  var totalBans = BanManager.GetBanHistory().Count;
        var activeBans = BanManager.GetActiveBans().Count;

        var embed = new EmbedBuilder()
            .WithTitle("📊 Sunucu Ban İstatistikleri")
            .WithColor(Color.Teal)
       //     .AddField("Toplam Ban Sayısı", totalBans.ToString(), false)
            .AddField("Aktif Ban Sayısı", activeBans.ToString(), false)
            .WithCurrentTimestamp();

        await command.RespondAsync(embed: embed.Build(), ephemeral: false);
    }
      
}