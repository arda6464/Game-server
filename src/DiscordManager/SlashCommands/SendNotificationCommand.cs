using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;


public static class SendNotificationCommand
{
    public static async Task HandleSendNotificationSlashAsync(SocketSlashCommand command)
    {
       var idOption = command.Data.Options
        .FirstOrDefault(opt => opt.Name == "id");
    
    if (idOption == null)
    {
        await command.RespondAsync("Bildirim türü belirtilmedi!", ephemeral: true);
        return;
    }
    
   NotficationTypes.NotficationType notificationType = (NotficationTypes.NotficationType)idOption.Value;
    Console.WriteLine($"Seçilen bildirim türü: {notificationType}");
        var playerIdOption = command.Data.Options
                    .FirstOrDefault(opt => opt.Name == "kullanıcıid");
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

        var Title = command.Data.Options
                   .FirstOrDefault(opt => opt.Name == "başlık");
        var Message = command.Data.Options
                   .FirstOrDefault(opt => opt.Name == "mesaj");

        if (Title == null || Message == null)
        {
            await command.RespondAsync("Başlık veya mesaj belirtilmedi!", ephemeral: false);
            return;
        }
        Notfication notfication;
     string Titlestr = Title.Value as string;
        string Messagestr = Message.Value as string;
     string ButtonNamestr = "";
     string ButtonLinkstr = "";
        if (notificationType == NotficationTypes.NotficationType.banner)
        {
            var ButtonName = command.Data.Options
                 .FirstOrDefault(opt => opt.Name == "butonadı");
            ButtonNamestr = ButtonName?.Value as string;
            var ButtonLink = command.Data.Options
               .FirstOrDefault(opt => opt.Name == "butonlinki");
            ButtonLinkstr = ButtonLink?.Value as string;


            notfication = new Notfication
            {
                type = notificationType,
                Title = Titlestr,
                Message = Messagestr,
                ButtonText = ButtonNamestr,
                Url = ButtonLinkstr
            };
        }
        else
        {
            notfication = new Notfication
            {
                type = notificationType,
                Title = Titlestr,
                Message = Messagestr
            };
        }
         NotificationManager.Add(account, notfication);
         Embed embed = new EmbedBuilder()
           .WithTitle("📨 Bildirim Gönderildi")
           .WithDescription($"Kullanıcı {playerId} bildirim Gönderildi!")
           .AddField("Başlık", Titlestr)
           .AddField("Mesaj", Messagestr)
           .WithCurrentTimestamp()
           .WithFooter("Bildirim Sistemi")
           .WithColor(Color.Blue)
           .Build();
         ButtonBuilder shownotficationButton = new ButtonBuilder()
         {
             Label = "Bildirim Geçmişini Göster",
             Style = ButtonStyle.Primary,
           CustomId = $"shownotfication_{playerId}_{2}"
         };
         await command.RespondAsync(embed: embed, ephemeral: false,  components: new ComponentBuilder().WithButton(shownotficationButton).Build());
         
    }
}