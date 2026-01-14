using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;

public static class banInfoCommand
{
     public static async Task GetBanİnfoSlashAsync(SocketSlashCommand command)
    {
        var playerIdOption = command.Data.Options
                    .FirstOrDefault(opt => opt.Name == "kullanıcı");
        string playerId = playerIdOption?.Value.ToString() ?? "Bilinmiyor";
        BanData data = BanManager.GetBanInfo(playerId);
        if (data == null)
        {
            await command.RespondAsync("Bu kullanıcı için ban bilgisi bulunamadı.", ephemeral: false);
            return;
        }
        var embed = new EmbedBuilder()
            .WithTitle($"⛔ {playerId} Ban Bilgisi")
            .WithColor(Color.DarkRed)
            .AddField("Sebep", data.Reason)
            .AddField("Banlayan", data.BannedBy)
            .AddField("Ban Tarihi", data.BanDate.ToString())
            .AddField("Süre", data.Perma ? "Kalıcı" : (data.BanFinishDate.HasValue ? data.BanFinishDate.Value.ToString() : "Belirtilmedi"))
            .AddField("Aktif", data.Active.ToString())
            .WithCurrentTimestamp();
        await command.RespondAsync(embed: embed.Build(), ephemeral: false);
    }

}