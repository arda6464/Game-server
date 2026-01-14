using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;
 
 public static class ActiveBanCommand
{
    public static async Task HandleActiveBansSlashAsync(SocketSlashCommand command)
    {
        // Aktif banlı kullanıcıları listeleme işlemi
        var activeBans = BanManager.GetActiveBans();

        if (activeBans.Count == 0)
        {
            await command.RespondAsync("Aktif banlı kullanıcı bulunmamaktadır.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("⛔ Aktif Banlı Kullanıcılar")
            .WithColor(Color.Orange);

        foreach (var ban in activeBans)
        {
            embed.AddField($"Kullanıcı ID: {ban.AccountId}", $"Sebep: {ban.Reason}\nBanlayan: {ban.BannedBy}\nSüre: {(ban.Perma ? "Kalıcı" : (ban.BanFinishDate.HasValue ? (ban.BanFinishDate.Value - DateTime.Now).ToString() : "Belirtilmedi"))}");
        }

        await command.RespondAsync(embed: embed.Build(), ephemeral: true);
    }
}