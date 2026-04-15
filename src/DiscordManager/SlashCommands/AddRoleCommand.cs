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
        int playerId = Convert.ToInt32(playerIdOption.Value);
        if (playerId == null)
        {
            await command.RespondAsync("Geçersiz kullanıcı ID'si!", ephemeral: false);
            return;
        }
        var logic = Logic.AccountLogic.Get(playerId);
        if (logic == null)
        {
            await command.RespondAsync("Bu ID'ye sahip bir hesap bulunamadı.", ephemeral: false);
            return;
        }
        
        logic.AddRole(role);
        Embed rolembed = new EmbedBuilder()
        .WithTitle("Yeni rol verildi!")
        .WithDescription($"oyuncu {logic.Data.Username} adlı kullanıcıya oyun içi {role} rolü verildi!")
        .WithCurrentTimestamp()
        .WithColor(Color.Green)
        .Build();
        await command.RespondAsync(embed: rolembed, ephemeral: false);

      
    }
}