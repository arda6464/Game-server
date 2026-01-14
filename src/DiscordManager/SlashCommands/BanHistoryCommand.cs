using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;


public static class BanHistoryCommand
{
    public static async Task HandleBanHistorySlashAsync(SocketSlashCommand command)
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
        var banHistory = BanManager.GetBanHistory(playerId);
        if (banHistory.Count == 0)
        {
            await command.RespondAsync("Bu kullanıcı için ban geçmişi bulunamadı.", ephemeral: false);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"📜 {account.Username} Ban Geçmişi")
            .WithColor(Color.Purple);

        foreach (var ban in banHistory)
        {
            embed.AddField($"Sebep: {ban.Reason}", $"Banlayan: {ban.BannedBy}\nTarih: {ban.BanDate}\nSüre: {(ban.Perma ? "Kalıcı" : (ban.BanFinishDate.HasValue ? (ban.BanFinishDate.Value - ban.BanDate).ToString() : "Belirtilmedi"))}\nAktif: {ban.Active}");
        }

        await command.RespondAsync(embed: embed.Build(), ephemeral: false);
    }
}