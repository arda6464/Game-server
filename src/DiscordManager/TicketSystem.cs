using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TicketData
{
    public int ID;
    public string? UserName;
    public int PlayerID;
    public Session? session;
    public ulong channelid;
}

public class Ticket
{
    private BotManager bot;
    
    // TicketGlobalID → SupportTicketData
    public Dictionary<int, SupportTicketData> tickets = new();
    
    // channelId → TicketGlobalID
    public Dictionary<ulong, int> channelToTicket = new();

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
        channelToTicket = TicketStorage.LoadChannelMappings();
        
        // Eğer mapping yoksa veya eksikse, ticket'lardan oluştur
        if (channelToTicket.Count == 0)
        {
            channelToTicket = TicketStorage.BuildChannelToAccountFromTickets(tickets);
            
            // Hemen kaydet
            TicketStorage.SaveChannelMappings(channelToTicket);
        }
    }

    // Ticket aç
    public void CreateTicket(int playerid, SupportTicketData data)
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

            var channel = await guild.CreateTextChannelAsync($"ticket-{data.Username}", x =>
            {
                x.CategoryId = CategoryId;
            });

            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteVarInt((int)MessageType.SupporCreateTicketResponse);
            buffer.WriteVarInt(data.NO);
            buffer.WriteString(data.Title ?? " ");

            byte[] response = buffer.ToArray();
            buffer.Dispose();
            
            TicketMessage message = new TicketMessage
            {
                Name = "Sistem",
                Message = $"Merhaba {data.Username}! Destek talebin açıldı. Yetkililerden gelecek mesajları beklemen yeterli!",
                time = DateTime.Now
            };
            data.ticketMessages.Add(message);

            if(SessionManager.IsOnline(playerid))
            {
                Session? session = SessionManager.GetSession(playerid);
                session?.Send(response);
            }

            data.channelid = channel.Id;
            await channel.ModifyAsync(x => x.Name = data.ID.ToString());
            tickets[data.ID] = data;
            channelToTicket[channel.Id] = data.ID;

            var embed = new EmbedBuilder()  
                .WithTitle("🎫 Destek Talebi Oluşturuldu")
                .WithDescription($"Merhaba {data.Username}, destek talebiniz oluşturuldu.")
                .AddField("Sebep", "Belirtilmedi")
                .AddField("Durum", "Açık")
                .AddField("Ticket ID", data.ID)
                .WithColor(Color.Green)
                .WithFooter($"Oluşturulma: {DateTime.UtcNow:dd.MM.yyyy HH:mm}")
                .Build();

            await channel.SendMessageAsync(embed: embed);
        });
    }

    // Ticket kapat
    public bool CloseTicketAsync(ulong channelId, string reason, int TICKETID = 0)
    {
        try
        {
            var ch = bot.Client.GetChannel(channelId) as SocketTextChannel;
            if (ch == null) return false;

            int ticketId = 0;
            if (channelToTicket.TryGetValue(channelId, out ticketId)) { }
            else if (TICKETID != 0)
            {
            // Alternatif: TICKETID ile bul
                ticketId = TICKETID;
            
            // ChannelId'yi dictionary'e ekle
                var ticket = GetTicketDataByTicketID(ticketId);
                if (ticket != null && ticket.channelid == channelId)
                {
                    channelToTicket[channelId] = ticketId;
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

            tickets.Remove(ticketId);
            channelToTicket.Remove(channelId);

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
                buffer.WriteVarInt((int)MessageType.SupportTicketClosed);
                buffer.WriteVarInt(ticketData.NO);
                buffer.WriteString(ticketData.ClosedReason);
                buffer.WriteVarInt((int)new DateTimeOffset(ticketData.ClosedAt).ToUnixTimeSeconds());
                
                if (SessionManager.IsOnline(ticketData.PlayerID))
                {
                    byte[] response = buffer.ToArray();
                    Session? session = SessionManager.GetSession(ticketData.PlayerID);
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

            if (channelToTicket.TryGetValue(msg.Channel.Id, out int ticketid))
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

                    var account = AccountCache.Load(ticketData.PlayerID);
                    if (account == null) return;
                    var accticket = account.Tickets.FirstOrDefault(t => t.ID == ticketData.ID);
                    if (accticket != null) accticket.ticketMessages.Add(message);
                  


                    ByteBuffer buffer = new ByteBuffer();
                    buffer.WriteVarInt((int)MessageType.SupportMessageResponse);
                    buffer.WriteVarInt(ticketData.NO);
                    buffer.WriteString(msg.Author.Username);
                    buffer.WriteString(msg.Content);
                    byte[] response = buffer.ToArray();
                    buffer.Dispose();
                    
                    if (SessionManager.IsOnline(ticketData.PlayerID))
                    {
                        Session? session = SessionManager.GetSession(ticketData.PlayerID);
                        session?.Send(response);
                    }
                }   
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Message handler crashed: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    public List<SupportTicketData> GetActiveTickets() => new List<SupportTicketData>(tickets.Values);

    public SupportTicketData GetTicketIDChannelId(ulong channelId)
    {
        if (channelToTicket.TryGetValue(channelId, out int ticketid))
        {
            tickets.TryGetValue(ticketid, out SupportTicketData? data);
            return data;
        }
        return null;
    }

    public SupportTicketData GetTicketDataByTicketID(int ticketid)
    {
        if (tickets.TryGetValue(ticketid, out SupportTicketData data)) return data;
        return null;
    }

    public async void SendTicketMessage(int playerid, string message, int ticketid)
    {
        try
        {
            var account = AccountCache.Load(playerid);
            if (account == null) return;

            var ticket = account.Tickets.FirstOrDefault(t => t.ID == ticketid);
            if (ticket == null)
            {
                Console.WriteLine($"❌ Ticket bulunamadı: PlayerID {playerid}, TicketID {ticketid}");
                return;
            }

            if(ticket.channelid == 0)
            {
                var guild = bot.Client.GetGuild(1289235591061307392);
                var newchannel = await guild.CreateTextChannelAsync($"ticket-{ticket.Username}", x => { x.CategoryId = CategoryId; });
                ticket.channelid = newchannel.Id;
                tickets[ticket.ID] = ticket;
                channelToTicket[newchannel.Id] = ticket.ID;

                var embed = new EmbedBuilder()
                    .WithTitle("🎫 Destek Talebi Oluşturuldu")
                    .WithDescription($"Merhaba {ticket.Username}, destek talebiniz oluşturuldu.")
                    .AddField("Sebep", "Belirtilmedi")
                    .AddField("Durum", "Açık")
                    .AddField("Ticket ID", ticket.ID)
                    .WithColor(Color.Green)
                    .WithFooter($"Oluşturulma: {DateTime.UtcNow:dd.MM.yyyy HH:mm}")
                    .Build();

                await newchannel.SendMessageAsync(embed: embed);
            }

            var channel = bot.Client.GetChannel(ticket.channelid) as SocketTextChannel;
            if (channel == null)
            {
                Console.WriteLine($"❌ Kanal bulunamadı: {ticket.channelid}");
                return;
            }
            
        // Mesajı gönder
            await channel.SendMessageAsync(message);
            Console.WriteLine($"✅ Mesaj Discord'a gönderildi: PlayerID {playerid}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Mesaj gönderme hatası: {ex.Message}");
        }
    }
}