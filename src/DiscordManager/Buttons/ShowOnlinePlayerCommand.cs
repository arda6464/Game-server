using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;


public static class show_online_players
{
    static int inpage = 1;
    static int totalpage = 3;
    public static async Task Show(SocketMessageComponent component)
    {

        var embed = new EmbedBuilder()
        .WithTitle("Aktif oyuncular!")
        .WithDescription("sunucudaki aktif oyuncular:")
         .WithColor(Color.DarkBlue)
         .WithFooter($"Toplam {SessionManager.GetCount}");
        if (SessionManager.GetCount() == 0) embed.AddField("-","aktif oyuncu bulanamadı...");
        foreach (var data in SessionManager.GetAllSessions())
        {
            var account = AccountCache.Load(data.AccountId);
            embed.AddField($"{account.Username} - {account.AccountId}", $"Kupa: {account.Trophy}", false);
        }
        var playerembed = embed.Build();
           
        
        
        await component.UpdateAsync(msg =>
    {
        msg.Embed = playerembed;
        // Geri dönüş butonu ekleyelim
        var newComponents = new ComponentBuilder()
            .WithButton(new ButtonBuilder()
            {
                Label = "⬅️ Geri Dön",
                Style = ButtonStyle.Secondary,
                CustomId = $"comeback_{inpage}" // Ana ekrana döner
            })
            .WithButton(new ButtonBuilder
            {
                Label = $"{inpage}/{totalpage}",
                CustomId = "pageinfo",
                Style = ButtonStyle.Secondary,
        IsDisabled = true
            })
            .WithButton(new ButtonBuilder
            {
                Label = "İleri git",
                Style = ButtonStyle.Secondary,
                CustomId = $"go_{inpage}"
            })
            .Build();
        msg.Components = newComponents;
    });
    }
}