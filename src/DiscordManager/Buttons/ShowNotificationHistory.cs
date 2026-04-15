using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;


public static class ShowNotificationHistory
{
    public static async Task HandleShowNotificationButton(SocketMessageComponent component)
    {
        // CustomId'den bilgileri ayır
        var parts = component.Data.CustomId.Split('_');
        if (parts.Length >= 2)
        {
            int playerId = Convert.ToInt32(parts[1]);
            var account = AccountCache.Load(playerId);
            if (account == null)
            {
                await component.RespondAsync("Bu ID'ye sahip bir hesap bulunamadı.", ephemeral: true);
                return;
            }
            // Bildirim geçmişini getir
            //  var notificationHistory = GetNotificationHistory(playerId);

            var historyEmbed = new EmbedBuilder()
                .WithTitle($"📜 {playerId} Bildirim Geçmişi")
                .WithDescription($"Son 10 bildirim:")
                .WithColor(Color.Purple);

            foreach (var notif in account.Notfications.TakeLast(10))
            {
                historyEmbed.AddField(
                  $"**{notif.Title}**\n{notif.Message}", $"{notif.Timespam:dd.MM.yyyy HH:mm} - {IdToString(notif.type.ToString())}");

            }

            // Butona tıklayan kullanıcıya özel göster (ephemeral)
            await component.RespondAsync(
                embed: historyEmbed.Build(),
                ephemeral: true
            );
        }
    }
    private static string IdToString(string id)
    {
        return id switch
        {
            "11" => "Toast",
            "10" => "Popup",
            "12" => "İnbox",
            _ => "Bilinmiyor",
        };
    }
}
