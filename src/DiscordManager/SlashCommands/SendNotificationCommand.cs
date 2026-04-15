using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System;

public static class SendNotificationCommand
{
    public static async Task HandleSendNotificationSlashAsync(SocketSlashCommand command)
    {
        var idOption = command.Data.Options.FirstOrDefault(opt => opt.Name == "id");
        if (idOption == null)
        {
            await command.RespondAsync("Bildirim türü belirtilmedi!", ephemeral: true);
            return;
        }

        var notificationType = (NotficationTypes.NotficationType)idOption.Value;
        var playerIdOption = command.Data.Options.FirstOrDefault(opt => opt.Name == "kullanıcıid");
        if (playerIdOption == null)
        {
            await command.RespondAsync("Kullanıcı ID'si belirtilmedi!", ephemeral: false);
            return;
        }

        int playerId = int.Parse(playerIdOption.Value.ToString());
        var logic = Logic.AccountLogic.Get(playerId);
        if (logic == null)
        {
            await command.RespondAsync("Bu ID'ye sahip bir hesap bulunamadı.", ephemeral: false);
            return;
        }

        var titleOption = command.Data.Options.FirstOrDefault(opt => opt.Name == "başlık");
        var messageOption = command.Data.Options.FirstOrDefault(opt => opt.Name == "mesaj");

        if (titleOption == null || messageOption == null)
        {
            await command.RespondAsync("Başlık veya mesaj belirtilmedi!", ephemeral: false);
            return;
        }

        string titlestr = titleOption.Value.ToString();
        string messagestr = messageOption.Value.ToString();

        Notfication notification;
        if (notificationType == NotficationTypes.NotficationType.banner)
        {
            var buttonName = command.Data.Options.FirstOrDefault(opt => opt.Name == "butonadı");
            var buttonLink = command.Data.Options.FirstOrDefault(opt => opt.Name == "butonlinki");

            notification = new Notfication
            {
                type = notificationType,
                Title = titlestr,
                Message = messagestr,
                ButtonText = buttonName?.Value?.ToString() ?? "",
                Url = buttonLink?.Value?.ToString() ?? ""
            };
        }
        else
        {
            notification = new Notfication
            {
                type = notificationType,
                Title = titlestr,
                Message = messagestr
            };
        }

        logic.AddNotification(notification);

        Embed embed = new EmbedBuilder()
            .WithTitle("📨 Bildirim Gönderildi")
            .WithDescription($"Kullanıcı {playerId} bildirim Gönderildi!")
            .AddField("Başlık", titlestr)
            .AddField("Mesaj", messagestr)
            .WithCurrentTimestamp()
            .WithFooter("Bildirim Sistemi")
            .WithColor(Color.Blue)
            .Build();

        ButtonBuilder showNotificationButton = new ButtonBuilder()
        {
            Label = "Bildirim Geçmişini Göster",
            Style = ButtonStyle.Primary,
            CustomId = $"shownotfication_{playerId}_{2}"
        };

        await command.RespondAsync(embed: embed, ephemeral: false, components: new ComponentBuilder().WithButton(showNotificationButton).Build());
    }
}