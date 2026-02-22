using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;

public class SlashCommands
{
    private BotManager bot;

    public SlashCommands(BotManager manager)
    {
        bot = manager;
    }
    // Komut tipleri:
         /*       ApplicationCommandOptionType.String      // Metin
                ApplicationCommandOptionType.Integer     // Sayı (tam sayı)
                ApplicationCommandOptionType.Number      // Sayı (ondalıklı)
                ApplicationCommandOptionType.Boolean     // True/False
                ApplicationCommandOptionType.User        // Kullanıcı seçimi
                ApplicationCommandOptionType.Channel     // Kanal seçimi
                ApplicationCommandOptionType.Role        // Rol seçimi
                ApplicationCommandOptionType.Mentionable // Kullanıcı veya Rol
                ApplicationCommandOptionType.Attachment  // Dosya ekleme*/
    // Global komutları kaydet
    public async Task RegisterGlobalCommandsAsync(ulong guildId = 0)
    {
              
        var guildCommand = new SlashCommandBuilder()
            .WithName("ticket")
            .WithDescription("Ticket yönetim sistemi")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("aç")
                .WithDescription("Yeni ticket açar")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("player-id",ApplicationCommandOptionType.String,"oyuncu ID numarası", isRequired: true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("kapat")
                .WithDescription("Ticket'ı kapatır")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("ticketid", ApplicationCommandOptionType.Integer, "Kapatılacak ticket ID'si", isRequired: false)
                .AddOption("sebep",ApplicationCommandOptionType.String,"Kapatılma sebebi",isRequired: false))
                .AddOption(new SlashCommandOptionBuilder()
                .WithName("liste")
                .WithDescription("Aktif ticket'ları listeler")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .Build();

        var helpCommand = new SlashCommandBuilder()
            .WithName("yardım")
            .WithDescription("Bot komutlarını gösterir")
            .Build();

        var BanCommand = new SlashCommandBuilder()
            .WithName("ban")
            .WithDescription("Belirtilen kullanıcıyı banlar")
            .AddOption("playerid", ApplicationCommandOptionType.String, "Banlanacak kullanıcı ID", isRequired: true)
            .AddOption("sebep", ApplicationCommandOptionType.String, "Ban sebebi", isRequired: false)
            .AddOption("kalıcı", ApplicationCommandOptionType.Boolean, "ban süresi kalıcımı", isRequired: true)
            .AddOption("süre", ApplicationCommandOptionType.String, "Ban süresi (gün:saat:dk:saniye)(perma ise 0)", isRequired: true)
            .Build();

        var UnbanCommand = new SlashCommandBuilder()
            .WithName("unban")
            .WithDescription("Belirtilen kullanıcının banını kaldırır")
            .AddOption("kullanıcı", ApplicationCommandOptionType.String, "Banı kaldırılacak kullanıcı ID", isRequired: true)
            .Build();


        var activeBanList = new SlashCommandBuilder()
            .WithName("aktifbanlar")
            .WithDescription("Aktif banlı kullanıcıları listeler")
            .Build();
        var banHistoryCommand = new SlashCommandBuilder()
            .WithName("bangeçmişi")
            .WithDescription("Ban geçmişini gösterir")
            .AddOption("kullanıcı", ApplicationCommandOptionType.String, "Ban geçmişi gösterilecek kullanıcı ID", isRequired: true)
            .Build();
        var banStatsCommand = new SlashCommandBuilder()
            .WithName("sunucubanistatistikleri")
            .WithDescription("Ban istatistiklerini gösterir")
            .Build();

        var AddDiscordAdminIDs = new SlashCommandBuilder()
            .WithName("addadmin")
            .WithDescription("Discord yönetici ID'si ekler")
            .AddOption("kullanıcı", ApplicationCommandOptionType.User, "Yönetici olarak eklenecek kullanıcı", isRequired: true)
            .Build();

        var RemoveDiscordAdminIDs = new SlashCommandBuilder()
           .WithName("removeadmin")
           .WithDescription("Discord yönetici ID'si kaldırır")
           .AddOption("kullanıcı", ApplicationCommandOptionType.User, "Yönetici olarak kaldırılacak kullanıcı", isRequired: true)
           .Build();

        var banInfoCommand = new SlashCommandBuilder()
            .WithName("baninfo")
            .WithDescription("Kullanıcının ban bilgilerini gösterir")
            .AddOption("kullanıcı", ApplicationCommandOptionType.String, "Ban bilgisi gösterilecek kullanıcı ID", isRequired: true)
            .Build();

        var ServerStatsCommand = new SlashCommandBuilder()
            .WithName("serverstats")
            .WithDescription("Sunucu istatistiklerini gösterir")
            .Build();

        var PingCommand = new SlashCommandBuilder()
            .WithName("ping")
            .WithDescription("Botun pingini gösterir")
            .Build();

        var TrafficCommand = new SlashCommandBuilder()
            .WithName("traffic")
            .WithDescription("Sunucu trafik analizini gösterir")
            .Build();

        var SendNotficationCommand = new SlashCommandBuilder()
            .WithName("sendnotification")
            .WithDescription("Belirtilen oyuncuya bildirim gönderir")
            .AddOption(new SlashCommandOptionBuilder()
            .WithName("id")
            .WithDescription("Bildirim gönderilecek tür")
            .WithType(ApplicationCommandOptionType.Integer)
            .AddChoice("Toast", (int)NotficationTypes.NotficationType.toast)
            .AddChoice("Popup", (int)NotficationTypes.NotficationType.banner)
            .AddChoice("İnbox",(int)NotficationTypes.NotficationType.Inbox)
            .AddChoice("Push",(int)NotficationTypes.NotficationType.Push)
            .WithRequired(true))
            

            .AddOption("kullanıcıid", ApplicationCommandOptionType.String, "Bildirim gönderilecek kullanıcı ID'si", isRequired: true)
            .AddOption("başlık", ApplicationCommandOptionType.String, "Bildirim başlığı", isRequired: true)
            .AddOption("mesaj", ApplicationCommandOptionType.String, "Gönderilecek bildirim mesajı", isRequired: true)
            .AddOption("butonadı", ApplicationCommandOptionType.String, "Bildirim buton adı", isRequired: false)
            .AddOption("butonlinki", ApplicationCommandOptionType.String, "Bildirim buton linki", isRequired: false)
            .Build();

        var SendAllNotificationCommand = new SlashCommandBuilder()
            .WithName("sendallnotification")
            .WithDescription("Tüm oyunculara bildirim gönderir")
             .AddOption(new SlashCommandOptionBuilder()
            .WithName("id")
            .WithDescription("Bildirim gönderilecek tür")
            .WithType(ApplicationCommandOptionType.Integer)
            .AddChoice("Toast", (int)NotficationTypes.NotficationType.toast)
            .AddChoice("Popup", (int)NotficationTypes.NotficationType.banner)
            .AddChoice("İnbox",(int)NotficationTypes.NotficationType.Inbox)
            .AddChoice("Push",(int)NotficationTypes.NotficationType.Push)
            .WithRequired(true))
            .AddOption("başlık", ApplicationCommandOptionType.String, "Bildirim başlığı", isRequired: true)
            .AddOption("mesaj", ApplicationCommandOptionType.String, "Gönderilecek bildirim mesajı", isRequired: true)
            .AddOption("butonadı", ApplicationCommandOptionType.String, "Bildirim buton adı", isRequired: false)
            .AddOption("butonlinki", ApplicationCommandOptionType.String, "Bildirim buton linki", isRequired: false)
            .Build();

                var rozetypeBuilder = new SlashCommandOptionBuilder()
            .WithName("rozetype")
            .WithDescription("Rozet türü")
            .WithType(ApplicationCommandOptionType.String)
            .WithRequired(true);
                var AddRoleCommand = new SlashCommandBuilder()
            .WithName("addrole")
            .WithDescription("Oyunda istediğin kişiye rozet verir")
            .AddOption("id", ApplicationCommandOptionType.String, "verilecek oyunucunun ID", isRequired: true)
            .AddOption(rozetypeBuilder);



                foreach (var role in Enum.GetNames(typeof(Role.Roles)))
                {
                    rozetypeBuilder.AddChoice(role, role.ToString());
                }
    
        



        try
        {
            if (guildId == 0)
            {
                await bot.Client.CreateGlobalApplicationCommandAsync(guildCommand);
                await bot.Client.CreateGlobalApplicationCommandAsync(helpCommand);
                await bot.Client.CreateGlobalApplicationCommandAsync(BanCommand);
                await bot.Client.CreateGlobalApplicationCommandAsync(UnbanCommand);
                await bot.Client.CreateGlobalApplicationCommandAsync(activeBanList);
                await bot.Client.CreateGlobalApplicationCommandAsync(banHistoryCommand);
                await bot.Client.CreateGlobalApplicationCommandAsync(banStatsCommand);
                await bot.Client.CreateGlobalApplicationCommandAsync(AddDiscordAdminIDs);
                await bot.Client.CreateGlobalApplicationCommandAsync(RemoveDiscordAdminIDs);
                await bot.Client.CreateGlobalApplicationCommandAsync(banInfoCommand);
                await bot.Client.CreateGlobalApplicationCommandAsync(ServerStatsCommand);
                await bot.Client.CreateGlobalApplicationCommandAsync(PingCommand);
                await bot.Client.CreateGlobalApplicationCommandAsync(TrafficCommand);
                await bot.Client.CreateGlobalApplicationCommandAsync(SendNotficationCommand);
                await bot.Client.CreateGlobalApplicationCommandAsync(SendAllNotificationCommand);
                await bot.Client.CreateGlobalApplicationCommandAsync(AddRoleCommand.Build());

                return;
            }
            var guild = bot.Client.GetGuild(guildId);
            if (guild == null)
            {
                Console.WriteLine("Guild bulunamadı!");
                return;
            }
            await guild.CreateApplicationCommandAsync(guildCommand);
            await guild.CreateApplicationCommandAsync(helpCommand);
            await guild.CreateApplicationCommandAsync(BanCommand);
            await guild.CreateApplicationCommandAsync(UnbanCommand);
            await guild.CreateApplicationCommandAsync(activeBanList);
            await guild.CreateApplicationCommandAsync(banHistoryCommand);
            await guild.CreateApplicationCommandAsync(banStatsCommand);
            await guild.CreateApplicationCommandAsync(AddDiscordAdminIDs);
            await guild.CreateApplicationCommandAsync(RemoveDiscordAdminIDs);
            await guild.CreateApplicationCommandAsync(banInfoCommand);
            await guild.CreateApplicationCommandAsync(ServerStatsCommand);
            await guild.CreateApplicationCommandAsync(PingCommand);
            await guild.CreateApplicationCommandAsync(TrafficCommand);
            await guild.CreateApplicationCommandAsync(SendNotficationCommand);
            await guild.CreateApplicationCommandAsync(SendAllNotificationCommand);
             await bot.Client.CreateGlobalApplicationCommandAsync(AddRoleCommand.Build());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Komut kaydetme hatası: {ex}");
        }
    }
    // Tüm komutları temizle
    public async Task ClearAllCommandsAsync(ulong? guildId = null)
    {
        try
        {
            if (guildId.HasValue)
            {
                // Belirli guild'in komutlarını sil
                var guild = bot.Client.GetGuild(guildId.Value);
                if (guild != null)
                {
                    var commands = await guild.GetApplicationCommandsAsync();
                    foreach (var cmd in commands)
                    {
                        await cmd.DeleteAsync();
                        Console.WriteLine($"🗑️ Silindi: {cmd.Name} (Guild)");
                    }
                }
            }
            else
            {
                // Global komutları sil
                var commands = await bot.Client.GetGlobalApplicationCommandsAsync();
                foreach (var cmd in commands)
                {
                    await cmd.DeleteAsync();
                    Console.WriteLine($"🗑️ Silindi: {cmd.Name} (Global)");
                }
            }

            Console.WriteLine("✅ Komutlar temizlendi!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Temizleme hatası: {ex.Message}");
        }
    }

    // Slash komutlarını işle
    public async Task ProcessAsync(SocketSlashCommand command)
    {
        switch (command.CommandName)
        {
            case "ticket":
                await HandleTicketSlashAsync(command);
                break;

            case "yardım":
                await helpCommand.HandleHelpSlashAsync(command);
                break;
            case "ban":
                await  BanCommand.HandleBanSlashAsync(command);
                break;
            case "unban":
                await UnbanCommand.HandleUnbanSlashAsync(command);
                break;
            case "aktifbanlar":
                await ActiveBanCommand.HandleActiveBansSlashAsync(command);
                break;
            case "bangeçmişi":
                await BanHistoryCommand.HandleBanHistorySlashAsync(command);
                break;
            case "sunucubanistatistikleri":
                await ServerBanStatsCommand.HandleServerBanStatsSlashAsync(command);
                break;
            case "baninfo":
                await banInfoCommand.GetBanİnfoSlashAsync(command);
                break;
            case "addadmin":
                await DiscordAdminCommand.HandleAddDiscordAdminSlashAsync(command);
                break;
            case "removeadmin":
                await DiscordAdminCommand.HandleRemoveDiscordAdminIDsAsync(command);
                break;
            case "serverstats":
                await ServerStatsCommand.HandleServerStatsSlashAsync(command);
                break;
            case "ping":
                await PingCommand.HandlePingSlashAsync(command,bot);
                break;
            case "traffic":
                await TrafficCommand.HandleTrafficSlashAsync(command, bot);
                break;
            case "sendnotification":
                await SendNotificationCommand.HandleSendNotificationSlashAsync(command);
                break;
            case "sendallnotification":
                await SendAllNotificationCommand.HandleSendAllNotificationSlashAsync(command);
                break;
            case "addrole":
                await AddRoleCommand.HandleAsync(command);
                break;
                default:
                await command.RespondAsync("Bilinmeyen komut!", ephemeral: true);
                break;
            
        }
    }

    private async Task HandleTicketSlashAsync(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First().Name;

        switch (subCommand)
        {
            case "kapat":
                var ticketOption = command.Data.Options.First().Options
           .FirstOrDefault(opt => opt.Name == "ticketid");
                var reasonoption = command.Data.Options.First().Options
                .FirstOrDefault(opt => opt.Name == "sebep");
                string reason = reasonoption.Value.ToString() ?? "sebep belirtilmedi.";
                bool result = bot.TicketSystem.CloseTicketAsync(command.Channel.Id,reason);
                if (result) await command.RespondAsync("Ticket kapatıldı!", ephemeral: false);
                else await command.RespondAsync("Ticket bulunamadı veya zaten kapatılmış!", ephemeral: false);
                break;
            case "liste":
                var tickets = bot.TicketSystem.GetActiveTickets();
                var listEmbed = new EmbedBuilder()
                    .WithTitle("📋 Aktif Ticket'lar")
                    .WithDescription(tickets.Count > 0 ? $"Toplam **{tickets.Count}** aktif ticket!" : "Aktif ticket bulunmamaktadır.")
                       .AddField("Ticket Listesi", tickets.Count > 0 ? string.Join("\n", tickets.Select(t => $"• ID: {t.AccountId}, Kullanıcı: {t.Username}")) : "—")
                    .WithColor(Color.Blue)
                    .Build();

                await command.RespondAsync(embed: listEmbed, ephemeral: false);
               
                break;
        }
    }  
}