using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;

public class TicketData
{
    public int ID;
    public string? UserName;
    public string? Accountid;
    public Session? session;
    public ulong channelid;
}

public class Ticket
{
    private BotManager bot;
    
    // accountId → TicketData
    public Dictionary<int, SupportTicketData> tickets = new();
    
    // channelId → accountId (hızlı erişim için)
    public Dictionary<ulong, int> channelToAccount = new();

    public ulong CategoryId = 1460265871082786878;

    public Ticket(BotManager manager)
    {
        bot = manager;
        tickets = TicketStorage.LoadTickets();
        LoadOrCreateChannelMappings();

    }

      private void LoadOrCreateChannelMappings()
    {
        // Önce dosyadan yükle
        channelToAccount = TicketStorage.LoadChannelMappings();
        
        // Eğer mapping yoksa veya eksikse, ticket'lardan oluştur
        if (channelToAccount.Count == 0)
        {
            channelToAccount = TicketStorage.BuildChannelToAccountFromTickets(tickets);
            
            // Hemen kaydet
            TicketStorage.SaveChannelMappings(channelToAccount);
        }
        
       
    }
    
 
        
     

    // Ticket aç
    public void CreateTicket(string accountId, SupportTicketData data)
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
            var channel = await guild.CreateTextChannelAsync($"ticket-{data.Username}", x =>
            {
                x.CategoryId = CategoryId;
            });



            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteInt((int)MessageType.SupporCreateTicketResponse);
            buffer.WriteByte((byte)data.NO);
            buffer.WriteString(data.Title ?? " ");

            byte[] response = buffer.ToArray();
            buffer.Dispose();
            TicketMessage message = new TicketMessage
            {
                Name = "Sistem",
                Message = $"Merhaba{data.Username}! destek  talebin açıldı. yetkililerden gelecek mesajları beklemen yeterli!",
                time = DateTime.Now
            };
            data.ticketMessages.Add(message);

            if(SessionManager.IsOnline(accountId))
            {
                Session? session = SessionManager.GetSession(accountId);
                 session?.Send(response);
            
            }

            // Verileri kaydet
            data.channelid = channel.Id;
          await channel.ModifyAsync(x => x.Name = data.ID.ToString());
            tickets[data.ID] = data;
            channelToAccount[channel.Id] =data.ID;

            var embed = new EmbedBuilder()  
                .WithTitle("🎫 Destek Talebi Oluşturuldu")
                .WithDescription($"Merhaba {data.Username}, destek talebiniz oluşturuldu.")
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
   public bool CloseTicketAsync(ulong channelId, string reason, int TICKETID = 0)
{
    try
    {
        var ch = bot.Client.GetChannel(channelId) as SocketTextChannel;
        if (ch == null) return false;

        int ticketId = 0;
        
        // 1. channelToAccount'dan bul
        if (channelToAccount.TryGetValue(channelId, out ticketId))
        {
            // Bulundu
        }
        else if (TICKETID != 0)
        {
            // Alternatif: TICKETID ile bul
            ticketId = TICKETID;
            
            // ChannelId'yi dictionary'e ekle
            var ticket = GetTicketDataByTicketID(ticketId);
            if (ticket != null && ticket.channelid == channelId)
            {
                channelToAccount[channelId] = ticketId;
            }
            else
            {
                Console.WriteLine("Ticket bulunamadı!");
                return false;
            }
        }
        else
        {
            Console.WriteLine("[closed ticket] channelto'da bulunmadı");
            return false;
        }

        // 2. TicketData'yı al
        var ticketData = GetTicketDataByTicketID(ticketId);
        if (ticketData == null)
        {
            Console.WriteLine("Ticket data bulunamadı!");
            return false;
        }

        // 3. Durumu güncelle
        ticketData.IsClosed = true;
        ticketData.ClosedAt = DateTime.Now;
        ticketData.ClosedReason = reason;

        // 4. Dictionary'leri temizle
        tickets.Remove(ticketId);  // tickets'tan sil
        channelToAccount.Remove(channelId);  // channelToAccount'tan sil

        // 5. Embed gönder
        var embed = new EmbedBuilder()
            .WithTitle("✅ Ticket Kapatıldı")
            .WithDescription("Bu ticket kapatıldı ve 24 saat içinde silinecektir.")
            .AddField("Kapatılma sebebi", reason)
            .WithColor(Color.Red)
            .WithFooter($"Kapatılma: {DateTime.Now:dd.MM.yyyy HH:mm}")
            .Build();
        
         ch.SendMessageAsync(embed: embed);

        // 6. Oyuna bildir
        using (ByteBuffer buffer = new ByteBuffer())
        {
            buffer.WriteInt((int)MessageType.SupportTicketClosed);
            buffer.WriteByte((byte)ticketData.NO);
            buffer.WriteString(ticketData.ClosedReason);
            buffer.WriteInt((int)new DateTimeOffset(ticketData.ClosedAt).ToUnixTimeSeconds());
            
            if (SessionManager.IsOnline(ticketData.AccountId))
            {
                byte[] response = buffer.ToArray();
                Session? session = SessionManager.GetSession(ticketData.AccountId);
                session?.Send(response);
            }
        }

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ticket kapatma hatası: {ex.Message}");
        return false;
    }
}
    public void OnDiscordMessage(SocketMessage msg)
    {
        try
        {
            if (msg.Author.IsBot) return;

            // Mesajın gönderildiği kanal bir ticket kanalı mı?
            if (channelToAccount.TryGetValue(msg.Channel.Id, out int ticketid))
            {
                // Bu kanala ait TicketData'yı getir
                if (tickets.TryGetValue(ticketid, out SupportTicketData? ticketData))
                {
                    Console.WriteLine($"📨 Ticket Mesajı:");
                    Console.WriteLine($"   Kanal: #{msg.Channel.Name}");
                    Console.WriteLine($"   Kullanıcı: {msg.Author.Username}");
                    Console.WriteLine($"   Mesaj: {msg.Content}");
                    Console.WriteLine($"   Ticket Data:");
                    Console.WriteLine($"     - UserName: {ticketData.Username}");



                    TicketMessage message = new TicketMessage
                    {
                        Name = msg.Author.GlobalName,
                        Message = msg.Content,
                        time = DateTime.Now
                    };
                    ticketData.ticketMessages.Add(message);

                    var acccount = AccountCache.Load(ticketData.AccountId);
                    if (acccount == null) return;
                    var accticket = acccount.Tickets.FirstOrDefault(t => t.ID == ticketData.ID);
                    if (accticket != null) accticket.ticketMessages.Add(message);
                  


                    ByteBuffer buffer = new ByteBuffer();
                    buffer.WriteInt((int)MessageType.SupportMessageResponse);
                    buffer.WriteByte((byte)ticketData.NO);
                    buffer.WriteString(msg.Author.Username);
                    buffer.WriteString(msg.Content);
                    byte[] response = buffer.ToArray();
                    buffer.Dispose();
                    if (SessionManager.IsOnline(ticketData.AccountId))
                    {
                        Session? session = SessionManager.GetSession(ticketData.AccountId);
                        session?.Send(response);
                    }
                
                }   
            }
            else
            {
               /* var textchannels = GetTextChannelsInCategory();
                foreach (var ch in textchannels)
                {
                //    ch.Id = da
                }*/
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Message handler crashed: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    public List<SupportTicketData> GetActiveTickets()
    {
        return new List<SupportTicketData>(tickets.Values);
    }
    public List<SocketTextChannel> GetTextChannelsInCategory()
{
    var guild = bot.Client.GetGuild(1289235591061307392);
    if (guild == null) return new List<SocketTextChannel>();
    
    var category = guild.GetCategoryChannel(CategoryId);
    if (category == null) return new List<SocketTextChannel>();
    
    return category.Channels
        .OfType<SocketTextChannel>()
        .ToList();
}




    // Ek metod: Kanal ID'sine göre TicketData getir
    public SupportTicketData GetTicketIDChannelId(ulong channelId)
    {
        if (channelToAccount.TryGetValue(channelId, out int ticketid))
        {
            tickets.TryGetValue(ticketid, out SupportTicketData? data);
            return data;
        }
        return null;
    }


    
    public SupportTicketData GetTicketDataByTicketID(int ticketid)
{
    if (tickets.TryGetValue(ticketid, out SupportTicketData data))
    {
        return data;
    }
    return null;
}

    // Ek metod: Tüm aktif ticket kanallarını listele
    public List<ulong> GetActiveTicketChannels()
    {
        return new List<ulong>(channelToAccount.Keys);
    }
   public async void SendTicketMessage(string accountId, string message,int ticketid)
{
    try
    {
            var account = AccountCache.Load(accountId);
            if (account == null) return;

            var ticket = account.Tickets.FirstOrDefault(t => t.ID == ticketid);
            if (ticket == null) return;


        if (ticket == null)
        {
            Console.WriteLine($"❌ Ticket bulunamadı: {accountId}");
            return;
        }
        if(ticket.channelid == 0)
            {
                 var guild = bot.Client.GetGuild(1289235591061307392);
                    var category = guild.GetCategoryChannel(CategoryId);
                var newchannel = await guild.CreateTextChannelAsync($"ticket-{ticket.Username}", x =>
        {
            x.CategoryId = CategoryId;

        });
                ticket.channelid = newchannel.Id;
                tickets[ticket.ID] = ticket;
            channelToAccount[newchannel.Id] =ticket.ID;
                 var embed = new EmbedBuilder()
                .WithTitle("🎫 Destek Talebi Oluşturuldu")
                .WithDescription($"Merhaba {ticket.Username}, destek talebiniz oluşturuldu.")
                .AddField("Sebep", "Belirtilmedi")
                .AddField("Durum", "Açık")
                .AddField("Ticket ID", ticketid)
                .WithColor(Color.Green)
                .WithFooter($"Oluşturulma: {DateTime.UtcNow:dd.MM.yyyy HH:mm}")
                .Build();

            await newchannel.SendMessageAsync(embed: embed);
            
            }
        // Channel ID'yi al
        ulong channelId = ticket.channelid;
        
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