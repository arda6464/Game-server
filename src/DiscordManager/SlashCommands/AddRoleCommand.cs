using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;
public static class AddRoleCommand
{
    public static async Task HandleAsync(SocketSlashCommand command)
    {
          var idOption = command.Data.Options
        .FirstOrDefault(opt => opt.Name == "rozetype");

        if (idOption == null)
        {
            await command.RespondAsync("Bildirim türü belirtilmedi!", ephemeral: true);
            return;
        }
        string rozetype = idOption.Value.ToString();

        Role.Roles role;
        
        if (Enum.TryParse(rozetype, out role))
        {
           
        }
        else
        {
            await command.RespondAsync("Geçersiz rozet değeri!", ephemeral: true);
            return;
        }
        
        var playerIdOption = command.Data.Options
                    .FirstOrDefault(opt => opt.Name == "id");
        if (playerIdOption == null)
        {
            await command.RespondAsync("Kullanıcı ID'si belirtilmedi!", ephemeral: false);
            return;
        }
        string playerId = playerIdOption.Value as string;
        if (playerId == null)
        {
            await command.RespondAsync("Geçersiz kullanıcı ID'si!", ephemeral: false);
            return;
        }
        var account = AccountCache.Load(playerId);
        if (account == null)
        {
            await command.RespondAsync("Bu ID'ye sahip bir hesap bulunamadı.", ephemeral: false);
            return;
        }

        AccountManager.AddRole(account, role);
        Embed rolembed = new EmbedBuilder()
        .WithTitle("Yeni rol verildi!")
        .WithDescription($"oyuncu {account.Username} adlı kullanıcıya oyun içi {role} rolü verildi!")
        .WithCurrentTimestamp()
        .WithColor(Color.Green)
        .Build();
        await command.RespondAsync(embed: rolembed, ephemeral: false);

      
    }
}