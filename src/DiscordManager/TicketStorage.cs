using System.Text.Json;

public static class TicketStorage
{
    private static readonly string StoragePath = "Data/tickets.json";
    private static readonly string MappingPath = "Data/channel_mappings.json";
    public static int MaxTicketID =1;
    
    public static void Initialize()
    {
        // Sadece ticket'ları yükle
        // channelToAccount'u sonra oluşturacağız
    }
    
    // Tüm verileri kaydet
    public static void SaveAllData(Dictionary<int, SupportTicketData> tickets, 
                                  Dictionary<ulong, int> channelToAccount)
    {
        try
        {
            // 1. Ticket'ları kaydet
            SaveTickets(tickets);
            
            // 2. Mapping'leri kaydet
            SaveChannelMappings(channelToAccount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Veri kaydetme hatası: {ex.Message}");
        }
    }
    
    // Ticket'ları kaydet
    public static void SaveTickets(Dictionary<int, SupportTicketData> tickets)
    {
        try
        {
            var data = tickets.Values
                .Where(t => t != null)
                .ToList();
                
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(StoragePath, json);
            Console.WriteLine($"✅ {data.Count} ticket kaydedildi");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ticket kaydetme hatası: {ex.Message}");
        }
    }
    
    // Channel mapping'leri kaydet
    public static void SaveChannelMappings(Dictionary<ulong, int> channelToAccount)
    {
        try
        {
            var data = channelToAccount
                .Select(kvp => new ChannelMapping 
                { 
                    ChannelId = kvp.Key, 
                    TicketId = kvp.Value 
                })
                .ToList();
                
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(MappingPath, json);
            Console.WriteLine($"✅ {data.Count} channel mapping kaydedildi");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Mapping kaydetme hatası: {ex.Message}");
        }
    }
    
    // Ticket'ları yükle
    public static Dictionary<int, SupportTicketData> LoadTickets()
    {
        var tickets = new Dictionary<int, SupportTicketData>();
        
        try
        {
            if (!File.Exists(StoragePath))
            {
                File.WriteAllText(StoragePath, "[]");
                return tickets;
            }
            
            string json = File.ReadAllText(StoragePath);
            var ticketList = JsonSerializer.Deserialize<List<SupportTicketData>>(json) 
                           ?? new List<SupportTicketData>();
            
            foreach (var ticket in ticketList)
            {
                if (ticket != null && ticket.ID > 0)
                {
                    tickets[ticket.ID] = ticket;
                    if (ticket.ID > MaxTicketID)
                        MaxTicketID = ticket.ID;
                }
            }
            
            Console.WriteLine($"✅ {tickets.Count} ticket yüklendi");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ticket yükleme hatası: {ex.Message}");
        }
        
        return tickets;
    }
    
    // Channel mapping'leri yükle
    public static Dictionary<ulong, int> LoadChannelMappings()
    {
        var mappings = new Dictionary<ulong, int>();
        
        try
        {
            if (!File.Exists(MappingPath))
            {
                File.WriteAllText(MappingPath, "[]");
                return mappings;
            }
            
            string json = File.ReadAllText(MappingPath);
            var mappingList = JsonSerializer.Deserialize<List<ChannelMapping>>(json) 
                            ?? new List<ChannelMapping>();
            
            foreach (var mapping in mappingList)
            {
                if (mapping.ChannelId > 0 && mapping.TicketId > 0)
                {
                    mappings[mapping.ChannelId] = mapping.TicketId;
                }
            }
            
            Console.WriteLine($"✅ {mappings.Count} channel mapping yüklendi");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Mapping yükleme hatası: {ex.Message}");
        }
        
        return mappings;
    }
    
    // Ticket'lardan channelToAccount oluştur
    public static Dictionary<ulong, int> BuildChannelToAccountFromTickets(
        Dictionary<int, SupportTicketData> tickets)
    {
        var mappings = new Dictionary<ulong, int>();
        
        foreach (var kvp in tickets)
        {
            var ticket = kvp.Value;
            
            // channelid varsa ekle
            if (ticket.channelid > 0)
            {
                mappings[ticket.channelid] = ticket.ID;
            }
            
            
        }
        
        Console.WriteLine($"🔗 {mappings.Count} mapping ticket'lardan oluşturuldu");
        return mappings;
    }
    
    [Serializable]
    public class ChannelMapping
    {
        public ulong ChannelId { get; set; }
        public int TicketId { get; set; }
    }
}
