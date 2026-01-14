using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;

public static class DiscordAdminCommand
{
    public static async Task HandleAddDiscordAdminSlashAsync(SocketSlashCommand command)
    {
        var userOption = command.Data.Options
                     .FirstOrDefault(opt => opt.Name == "kullanıcı");
        var user = userOption?.Value as SocketUser;
        if (user == null)
        {
            await command.RespondAsync("Kullanıcı belirtilmedi!", ephemeral: false);
            return;
        }
        if (Config.Instance.DiscordAdminIDs.Contains(user.Id))
        {
            await command.RespondAsync("Bu kullanıcı zaten yönetici!", ephemeral: false);
            return;
        }
        Config.AddAdmin(user.Id);
        BotManager.istance?.LoadAdminIDs().GetAwaiter().GetResult();

        await command.RespondAsync($"Kullanıcı {user.Username} yönetici olarak eklendi!", ephemeral: false);
    }
    
       public static async Task HandleRemoveDiscordAdminIDsAsync(SocketSlashCommand command)
    {
        var userOption = command.Data.Options
                    .FirstOrDefault(opt => opt.Name == "kullanıcı");
        var user = userOption?.Value as SocketUser;
        if (user == null)
        {
            await command.RespondAsync("Kullanıcı belirtilmedi!", ephemeral: false);
            return;
        }
        if (!Config.Instance.DiscordAdminIDs.Contains(user.Id))
        {
            await command.RespondAsync("Bu kullanıcı yönetici değil!", ephemeral: false);
            return;
        }
        Config.RemoveAdmin(user.Id);
        BotManager.istance?.LoadAdminIDs().GetAwaiter().GetResult();

        await command.RespondAsync($"Kullanıcı {user.Username} yönetici olarak kaldırıldı!", ephemeral: false);
    }

}