using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;


public static class SendAllNotificationCommand
{
      public static async Task HandleSendAllNotificationSlashAsync(SocketSlashCommand command)
    {

        var Message = command.Data.Options
                   .FirstOrDefault(opt => opt.Name == "sebep");
        var Title = command.Data.Options
                   .FirstOrDefault(opt => opt.Name == "başlık");
        var ButtonName = command.Data.Options
                   .FirstOrDefault(opt => opt.Name == "butonadı");
        var ButtonLink = command.Data.Options
                   .FirstOrDefault(opt => opt.Name == "butonlinki");


        var accounts = AccountCache.GetAllAccounts();
        foreach (var account in accounts)
        {
           
        }
    }
}