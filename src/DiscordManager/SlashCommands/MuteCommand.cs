using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

public static class MuteCommand
{
    public static async Task HandleMuteSlashAsync(SocketSlashCommand command)
    {
        var playerIdOption = command.Data.Options.FirstOrDefault(opt => opt.Name == "playerid");
        var minutesOption = command.Data.Options.FirstOrDefault(opt => opt.Name == "dakika");

        if (playerIdOption == null || minutesOption == null)
        {
            await command.RespondAsync("Eksik bilgi: Player ID veya Dakika belirtilmedi!", ephemeral: true);
            return;
        }

        int playerId = int.Parse(playerIdOption.Value.ToString());
        if (!int.TryParse(minutesOption.Value.ToString(), out int minutes))
        {
            await command.RespondAsync("Geçersiz dakika değeri!", ephemeral: true);
            return;
        }

        var logic = Logic.AccountLogic.Get(playerId);
        if (logic == null)
        {
            await command.RespondAsync("Bu ID'ye sahip bir hesap bulunamadı.", ephemeral: true);
            return;
        }
        
        logic.Mute(TimeSpan.FromMinutes(minutes));

        Embed embed = new EmbedBuilder()
            .WithTitle("🔇 Kullanıcı Susturuldu")
            .WithDescription($"{logic.Data.Username} ({playerId}) başarıyla susturuldu.")
            .AddField("Süre", $"{minutes} dakika")
            .AddField("Susturan", command.User.Username)
            .WithCurrentTimestamp()
            .WithColor(Color.Orange)
            .Build();

        await command.RespondAsync(embed: embed);
    }

    public static async Task HandleUnmuteSlashAsync(SocketSlashCommand command)
    {
        var playerIdOption = command.Data.Options.FirstOrDefault(opt => opt.Name == "playerid");
        if (playerIdOption == null)
        {
            await command.RespondAsync("Kullanıcı ID'si belirtilmedi!", ephemeral: true);
            return;
        }

        int playerId = int.Parse(playerIdOption.Value.ToString());
        var logic = Logic.AccountLogic.Get(playerId);
        if (logic == null)
        {
            await command.RespondAsync("Bu ID'ye sahip bir hesap bulunamadı.", ephemeral: true);
            return;
        }
        
        if (!logic.IsMuted())
        {
            await command.RespondAsync("Bu kullanıcı zaten susturulmuş değil.", ephemeral: true);
            return;
        }

        logic.Unmute();

        Embed embed = new EmbedBuilder()
            .WithTitle("🔊 Susturma Kaldırıldı")
            .WithDescription($"{logic.Data.Username} ({playerId}) susturması kaldırıldı.")
            .AddField("İşlemi Yapan", command.User.Username)
            .WithCurrentTimestamp()
            .WithColor(Color.Green)
            .Build();

        await command.RespondAsync(embed: embed);
    }
}
