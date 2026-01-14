using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;

public static class BanCommand
{
   
    public static async Task HandleBanSlashAsync(SocketSlashCommand command)
    {
      
        var playerIdOption = command.Data.Options
                    .FirstOrDefault(opt => opt.Name == "playerid");
        if (playerIdOption == null)
        {
            await command.RespondAsync("Kullanıcı ID'si belirtilmedi!", ephemeral: false);
            return;
        }
        string playerId = playerIdOption?.Value.ToString() ?? "Bilinmiyor";

        if (playerId.Length != 8)
        {
            await command.RespondAsync("Geçersiz player ID. Lütfen doğru bir ID girin.", ephemeral: false);
            return;
        }
        var account = AccountCache.Load(playerId);
        if (account == null)
        {
            await command.RespondAsync("Bu ID'ye sahip bir hesap bulunamadı.", ephemeral: false);
            return;
        }
        var reasonOption = command.Data.Options
                    .FirstOrDefault(opt => opt.Name == "sebep");
        string reason = reasonOption?.Value.ToString() ?? "Belirtilmedi";
        var permaOption = command.Data.Options
                    .FirstOrDefault(opt => opt.Name == "kalıcı");
        bool perma = (bool)(permaOption?.Value ?? false);
        var durationOption = command.Data.Options
                    .FirstOrDefault(opt => opt.Name == "süre");
        string durationStr = durationOption?.Value.ToString() ?? "0";
        if (perma)
            BanManager.BanPlayer(playerId, command.User.Username, reason, true);
        else
        {
            if (!TimeSpan.TryParse(durationStr, out TimeSpan duration))
            {
                await command.RespondAsync("Geçersiz süre formatı. Lütfen doğru bir süre girin.", ephemeral: true);
                return;
            }
            BanManager.BanPlayer(playerId, command.User.Username, reason, false, duration);
        }
        Embed banembed = new EmbedBuilder()
         .WithTitle("⛔ Kullanıcı Banlandı")
         .WithDescription($"Kullanıcı {playerId} banlandı!")
         .AddField("Sebep", reason)
         .AddField("Banlayan", command.User.Username)
         .AddField("Süre", perma ? "Kalıcı" : durationStr)
         .AddField("açılma zamanı", perma ? "Yok" : (DateTime.Now + TimeSpan.Parse(durationStr)).ToString())
         .WithCurrentTimestamp()
         .WithFooter("Ban Sistemi")
         .WithColor(Color.Red)
         .Build();
        await command.RespondAsync(embed: banembed, ephemeral: false);
    }
}