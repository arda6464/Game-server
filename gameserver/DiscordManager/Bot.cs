using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

public class BotManager
{
    public DiscordSocketClient Client;
    public Commands Cmd;
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
                // MESAJ İÇERİĞİNİ OKUYABİLMESİ İÇİN:
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
                LogLevel = LogSeverity.Info
            };

            Client = new DiscordSocketClient(config);
            Cmd = new Commands(this);
            TicketSystem = new Ticket(this);
            Client.Log += LogAsync;
            Client.Ready += ReadyAsync;
            Client.MessageReceived += MessageReceivedAsync;

            string token = "MTI1NDUzNzE0ODQ5MzcyNTc3Ng.GXrouV.hCZGVm5hse3_t-kPumF24lfoTco88x4dL0CnkA";

            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

            Console.WriteLine("🤖 Bot çalışıyor...");

            // Bot'u açık tut
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Bot hatası: {ex.Message}");
        }
    }

private async Task MessageReceivedAsync(SocketMessage message)
{
    // 1. Bot mesajlarını yok say
    if (message.Author.IsBot) return;
    
         TicketSystem.OnDiscordMessage(message);

    
    // 3. Komut kontrolü
    if (message.Content.StartsWith("!"))
    {
        Console.WriteLine($"Komut algılandı: {message.Content}");
            Cmd.Process(message);
    }
}

private Task ReadyAsync()
{
    Console.WriteLine($"✅ Bot bağlandı: {Client.CurrentUser.Username}");
    return Task.CompletedTask;
}

private Task LogAsync(LogMessage log)
{
    Console.WriteLine($"[Discord] {log.Severity}: {log.Message} ex:{log.Exception}\n tosting: {log.Exception.ToString()} trace: {log.Exception.StackTrace}");
    return Task.CompletedTask;
}
}
