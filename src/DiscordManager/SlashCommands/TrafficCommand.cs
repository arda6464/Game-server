using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

public static class TrafficCommand
{
    public static async Task HandleTrafficSlashAsync(SocketSlashCommand command, BotManager bot)
    {
        string report = TrafficMonitor.GetReport();
        
        // Discord mesaj limiti 2000 karakterdir. Rapor uzunsa parçalamak gerekebilir ama şimdilik kod bloğu içine alalım.
        if (report.Length > 1900)
        {
            report = report.Substring(0, 1850) + "\n... (Rapor çok uzun, geri kalanı kesildi)";
        }

        var embed = new EmbedBuilder()
            .WithTitle("📊 Sunucu Trafik Analizi")
            .WithDescription("```" + report + "```")
            .WithColor(Color.Blue)
            .WithCurrentTimestamp()
            .WithFooter("Sıfırlamak için /traffic reset kullanın (yakında)")
            .Build();

        await command.RespondAsync(embed: embed, ephemeral: false);
    }
}
