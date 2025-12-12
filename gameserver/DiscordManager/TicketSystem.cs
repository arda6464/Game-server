using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;

public class TicketData
{
    public string? UserName;
    public string? Accountid;
    public Session? session;
    public ulong channelid;
}

public class Ticket
{
    private BotManager bot;
    
    // accountId → TicketData
    private Dictionary<string, TicketData> tickets = new();
    
    // channelId → accountId (hızlı erişim için)
    private Dictionary<ulong, string> channelToAccount = new();

    public ulong CategoryId = 1449140522441506898;

    public Ticket(BotManager manager)
    {
        bot = manager;
    }

    // Ticket aç
    public void CreateTicket(string accountId, TicketData data)
    {
        Task.Run(async () =>
        {
            var guild = bot.Client.GetGuild(1289235591061307392);
            var category = guild.GetCategoryChannel(CategoryId);

            if (category == null)
            {
                Console.WriteLine("Kategori bulunamadı!");
                return;
            }

            // Kanal oluştur
            var channel = await guild.CreateTextChannelAsync($"ticket-{data.UserName}", x =>
            {
                x.CategoryId = CategoryId;
            });
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteInt((int)MessageType.SupporCreateTicketResponse);
            data.session.Send(buffer.ToArray());
            buffer.Dispose();
            // Verileri kaydet
            data.channelid = channel.Id;
            tickets[accountId] = data;
            channelToAccount[channel.Id] = accountId;

            var embed = new EmbedBuilder()
                .WithTitle("🎫 Destek Talebi Oluşturuldu")
                .WithDescription($"Merhaba {data.UserName}, destek talebiniz oluşturuldu.")
                .AddField("Sebep", "Belirtilmedi")
                .AddField("Durum", "Açık")
                .AddField("Ticket ID", channel.Id)
                .WithColor(Color.Green)
                .WithFooter($"Oluşturulma: {DateTime.UtcNow:dd.MM.yyyy HH:mm}")
                .Build();

            await channel.SendMessageAsync(embed: embed);

        }).GetAwaiter().GetResult();
    }

    // Ticket kapat
    public void CloseTicket(ulong channelId)
    {
        Task.Run(async () =>
        {
            var ch = bot.Client.GetChannel(channelId) as SocketTextChannel;
            if (ch != null)
            {
                // channelToAccount'dan kaldır
                if (channelToAccount.ContainsKey(channelId))
                {
                    var accountId = channelToAccount[channelId];
                    tickets.Remove(accountId);
                    channelToAccount.Remove(channelId);
                }
                
                await ch.SendMessageAsync("Ticket kapatıldı.");
                await ch.DeleteAsync();
            }
        }).GetAwaiter().GetResult();
    }

    // Discord mesajlarını dinle - DEĞİŞTİRİLDİ
    public void OnDiscordMessage(SocketMessage msg)
    {
        try
        {
            if (msg.Author.IsBot) return;
            
            // Mesajın gönderildiği kanal bir ticket kanalı mı?
            if (channelToAccount.TryGetValue(msg.Channel.Id, out string accountId))
            {
                // Bu kanala ait TicketData'yı getir
                if (tickets.TryGetValue(accountId, out TicketData ticketData))
                {
                    Console.WriteLine($"📨 Ticket Mesajı:");
                    Console.WriteLine($"   Kanal: #{msg.Channel.Name}");
                    Console.WriteLine($"   Kullanıcı: {msg.Author.Username}");
                    Console.WriteLine($"   Mesaj: {msg.Content}");
                    Console.WriteLine($"   Ticket Data:");
                    Console.WriteLine($"     - UserName: {ticketData.UserName}");
                    Console.WriteLine($"     - AccountId: {ticketData.Accountid}");
                    Console.WriteLine($"     - Session: {ticketData.session}");

                    ByteBuffer buffer = new ByteBuffer();
                    buffer.WriteInt((int)MessageType.SupportMessageResponse);
                    buffer.WriteString(msg.Author.Username);
                    buffer.WriteString(msg.Content);
                    byte[] response = buffer.ToArray();
                    buffer.Dispose();
                    ticketData.session.Send(response);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Message handler crashed: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }
    
    
   
    
    // Ek metod: Kanal ID'sine göre TicketData getir
    public TicketData GetTicketDataByChannelId(ulong channelId)
    {
        if (channelToAccount.TryGetValue(channelId, out string accountId))
        {
            tickets.TryGetValue(accountId, out TicketData data);
            return data;
        }
        return null;
    }
    
    // Ek metod: Account ID'sine göre TicketData getir
    public TicketData GetTicketDataByAccountId(string accountId)
    {
        tickets.TryGetValue(accountId, out TicketData data);
        return data;
    }

    // Ek metod: Tüm aktif ticket kanallarını listele
    public List<ulong> GetActiveTicketChannels()
    {
        return new List<ulong>(channelToAccount.Keys);
    }
   public async void SendTicketMessage(string accountId, string message)
{
    try
    {
        var ticketData = GetTicketDataByAccountId(accountId);
        
        if (ticketData == null)
        {
            Console.WriteLine($"❌ Ticket bulunamadı: {accountId}");
            return;
        }
        
        // Channel ID'yi al
        ulong channelId = ticketData.channelid;
        
        // Kanalı bul
        var channel = bot.Client.GetChannel(channelId) as SocketTextChannel;
        
        if (channel == null)
        {
            Console.WriteLine($"❌ Kanal bulunamadı: {channelId}");
            return;
        }
        
        // Mesajı gönder
        await channel.SendMessageAsync(message);
        Console.WriteLine($"✅ Mesaj gönderildi: {accountId} -> #{channel.Name}");
        
        // TicketData'ya mesajı ekleyebilirsiniz
        
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Mesaj gönderme hatası: {ex.Message}");
    }
}
}