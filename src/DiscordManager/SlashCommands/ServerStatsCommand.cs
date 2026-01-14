using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;

public static class ServerStatsCommand
{
    public static async Task HandleServerStatsSlashAsync(SocketSlashCommand command)
    {
        // Sunucu istatistiklerini gösterme işlemi
        int onlinePlayers = SessionManager.GetCount();
        // int totalPlayers = ServerManager.GetTotalPlayerCount();
        DateTime startTime = Process.GetCurrentProcess().StartTime;
        DateTime now = DateTime.Now;
        TimeSpan uptime = now - startTime;
        string formattedUptime = string.Format(
            "{0}{1}{2}{3}",
            uptime.Days > 0 ? $"{uptime.Days} Gün, " : string.Empty,
            uptime.Hours > 0 || uptime.Days > 0 ? $"{uptime.Hours} Saat, " : string.Empty,
            uptime.Minutes > 0 || uptime.Hours > 0 ? $"{uptime.Minutes} Dakika, " : string.Empty,
            uptime.Seconds > 0 ? $"{uptime.Seconds} Saniye" : string.Empty);

        var embed = new EmbedBuilder()
            .WithTitle("📊 Sunucu İstatistikleri")
            .WithColor(Color.Gold)
            .AddField("Çevrimiçi Oyuncular", onlinePlayers.ToString(), false)
            .AddField("Toplam Oyuncular", AccountCache.Count, false)
            .AddField("Sunucu Çalışma Süresi", formattedUptime, false)
            .WithCurrentTimestamp();
         ButtonBuilder playershowbtn = new ButtonBuilder()
         {
             Label = "Çevrimiçi Oyuncuları Göster",
             Style = ButtonStyle.Primary,
             CustomId = "show_online_players",
        IsDisabled = onlinePlayers == 0

         };
        ButtonBuilder systeminfoButton = new ButtonBuilder()
        {
            Label = "Sistem Bilgilerini Göster",
            Style = ButtonStyle.Secondary,
            CustomId = "show_system_info"
        };

        var component = new ComponentBuilder()
            .WithButton(playershowbtn)
            .WithButton(systeminfoButton)
            .Build();
        

        await command.RespondAsync(embed: embed.Build(), components: component, ephemeral: false);
    }
}