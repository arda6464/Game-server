using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;

public static class UnbanCommand
{
    public static async Task HandleUnbanSlashAsync(SocketSlashCommand command)
    {
        var playerIdOption = command.Data.Options
                    .FirstOrDefault(opt => opt.Name == "kullanıcı");
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
        if (!BanManager.IsBanned(playerId))
        {
            await command.RespondAsync("Bu kullanıcı banlı değil!", ephemeral: false);
            return;
        }
        BanManager.UnbanPlayer(playerId, command.User.Username);
        Embed unbanembed = new EmbedBuilder()
         .WithTitle("✅ Kullanıcı Ban Kaldırıldı")
         .WithDescription($"Kullanıcı {playerId} banı kaldırıldı!")
         .WithCurrentTimestamp()
         .WithFooter("Ban Sistemi")
         .WithColor(Color.Green)
         .Build();
        await command.RespondAsync(embed: unbanembed, ephemeral: false);
    }
}