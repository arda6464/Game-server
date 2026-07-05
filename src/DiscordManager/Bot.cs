using Discord;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;

public class BotManager
{
    public DiscordSocketClient Client;
    public PrefixCommands PrefixCmd;
    public SlashCommands SlashCmd;
    public Ticket TicketSystem;
    public static BotManager istance;

    public BotManager()
    {
        istance = this;
    }

    public async Task Start()
    {
        try
        {
            var config = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.All,
    LogLevel = LogSeverity.Error
};

            Client = new DiscordSocketClient(config);
            PrefixCmd = new PrefixCommands(this);
            SlashCmd = new SlashCommands(this);
            TicketSystem = new Ticket(this);

            Client.Log += LogAsync;
            Client.Ready += ReadyAsync;
            Client.Ready += LoadAdminIDs;
            Client.MessageReceived += MessageReceivedAsync;
            Client.SlashCommandExecuted += SlashCommandExecutedAsync;
            Client.Ready += RegisterCommandsAsync;
            Client.ButtonExecuted += ButtonExecutedAsync;
            Client.SelectMenuExecuted += SelectMenuExecutedAsync; // Opsiyonel: SelectMenu için

            await Client.LoginAsync(TokenType.Bot, Config.Instance.BotToken);
            await Client.StartAsync();

            Console.WriteLine("🤖 Bot çalışıyor...");
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Bot hatası: {ex.Message}");
        }
    }
    

    private async Task RegisterCommandsAsync()
    {
        try
        {
            // Global komutları kaydet (tüm sunucularda çalışır)
            await SlashCmd.RegisterGlobalCommandsAsync();

            // Veya belirli sunucu için (daha hızlı)

            var guild = Client.GetGuild(1289235591061307392);
        //   Console.WriteLine("🧹 Eski komutlar temizleniyor...");
       // await SlashCmd.ClearAllCommandsAsync(guild.Id);
        
      // await Task.Delay(2000); // 2 saniye bekle
        
     //        await SlashCmd.RegisterGlobalCommandsAsync(guild.Id);
            
            Console.WriteLine("✅ Slash komutları kaydedildi!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Komut kaydetme hatası: {ex}");
        }
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
         
        if (message.Author.IsBot) return;
       
        TicketSystem.OnDiscordMessage(message);

        // Prefix komut kontrolü (! ile başlayan)
        if (message.Content.StartsWith("!"))
        {
            Console.WriteLine($"Prefix komut: {message.Content}");
            await PrefixCmd.ProcessAsync(message);
        }
    }
    public List<ulong> AdminIDs = new List<ulong>();
    public async Task<Task> LoadAdminIDs()
    {
        AdminIDs.Clear();
        try
        {
            
           
           List<ulong> defult = new List<ulong>();
            foreach (var id in Config.Instance.DiscordAdminIDs)
            {

                defult.Add(id);
                Console.WriteLine($"Yönetici ID'si yüklendi: {id}");

            }
            AdminIDs = defult;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Yönetici ID'leri yüklenirken hata: {ex.Message}");
        }
         return Task.CompletedTask;
    }

    private async Task SlashCommandExecutedAsync(SocketSlashCommand command)
    {
        Console.WriteLine($"Slash komut: {command.CommandName}");
        await SlashCmd.ProcessAsync(command);
    }

    private Task ReadyAsync()
    {
        Console.WriteLine($"✅ Bot bağlandı: {Client.CurrentUser.Username}");
        return Task.CompletedTask;
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine($"[Discord] {log.Severity}: {log.Message}");
        return Task.CompletedTask;
    }
    public bool IsAdmin(SocketUser user)
    {
        return AdminIDs.Contains(user.Id);
    }
     private async Task ButtonExecutedAsync(SocketMessageComponent component)
    {
        try
        {
            Console.WriteLine($"🔘 Buton tıklandı: {component.Data.CustomId}");
            
            // Kullanıcı ve mesaj bilgilerini al
            var user = component.User;
            var message = component.Message;
            
            // CustomId'yi parçala
            var customId = component.Data.CustomId;

            // Buton türüne göre işle
            if (customId.StartsWith("showNotification_"))
            {
                await ShowNotificationHistory.HandleShowNotificationButton(component);
            }
            else if (customId.StartsWith("show_online_players"))
            {
                await show_online_players.Show(component);
                await component.DeferAsync();
            }
            else if (customId.StartsWith("show_system_info"))
            {
                await SystemInfoCommand.ShowSystemInfoAsync(component);
                await component.DeferAsync();
            }
            else if (customId.StartsWith("show_ram_details"))
            {
                await SystemInfoCommand.ShowRamDetailsAsync(component);
                await component.DeferAsync();
            }
            else if(customId.StartsWith("show_cpu_details"))
            {
                await SystemInfoCommand.ShowCpuDetailsAsync(component);
                await component.DeferAsync();
            }
            /* else if (customId.StartsWith("confirm_notification_"))
             {
                 await HandleConfirmNotificationButton(component);
             }
             else if (customId.StartsWith("cancel_notification_"))
             {
                 await HandleCancelNotificationButton(component);
             }
             else if (customId.StartsWith("show_history_"))
             {
                 await HandleShowHistoryButton(component);
             }
             else if (customId.StartsWith("resend_"))
             {
                 await HandleResendButton(component);
             }
             // Ticket butonları
             else if (customId.StartsWith("ticket_"))
             {
                 await HandleTicketButton(component);
             }
             // Ban butonları
             else if (customId.StartsWith("ban_"))
             {
                 await HandleBanButton(component);
             }*/
            else
            {
                Console.WriteLine($"Bilinmeyen buton: {customId}");
                await component.RespondAsync(
                    "⚠️ Bu buton geçersiz veya süresi dolmuş!",
                    ephemeral: true
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Buton işleme hatası: {ex.Message}");
            
            // Hata durumunda kullanıcıya bilgi ver
            if (!component.HasResponded)
            {
                await component.RespondAsync(
                    "❌ Bir hata oluştu! Lütfen daha sonra tekrar deneyin.",
                    ephemeral: true
                );
            }
        }
    }

    // SELECT MENU EVENT'I (Opsiyonel)
    private async Task SelectMenuExecutedAsync(SocketMessageComponent component)
    {
        try
        {
            var customId = component.Data.CustomId;
            var selectedValues = component.Data.Values;
            
            
            Console.WriteLine($"📋 SelectMenu tıklandı: {customId} - Seçilen: {string.Join(", ", selectedValues)}");
            
            if (customId == "notification_type_select")
            {
                //await HandleNotificationTypeSelect(component, selectedValues);
            }
            // Diğer select menüler...
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SelectMenu hatası: {ex.Message}");
        }
    }
}