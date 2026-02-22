using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace StressTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== SARSILMAZ SUNUCU STRES TESTI BAŞLIYOR ===");
            Console.WriteLine("50 Bot simüle edilecek...");

            int botCount = 50;
            Task[] bots = new Task[botCount];

            for (int i = 0; i < botCount; i++)
            {
                int id = i;
                bots[i] = RunBot(id);
                await Task.Delay(100); // Bağlantıları hafifçe yayalım
            }

            await Task.WhenAll(bots);
        }

        static async Task RunBot(int botId)
        {
            try
            {
                using TcpClient client = new TcpClient("127.0.0.1", 5000);
                using NetworkStream stream = client.GetStream();
                
                // 1. Giriş Yap (Boş token ile yeni hesap oluşturma simülasyonu)
                await SendLogin(stream);
                
                Random rnd = new Random();
                
                while (true)
                {
                    // Her 1-4 saniyede bir rastgele paket gönder
                    await Task.Delay(rnd.Next(1000, 4000));
                    
                    int action = rnd.Next(0, 4);
                    switch (action)
                    {
                        case 0: await SendPing(stream); break;
                        case 1: await SendMatchmaking(stream); break;
                        case 2: await SendCreateTeam(stream); break;
                        case 3: await SendClubMessage(stream, botId); break;
                    }
                    
                    // Sunucudan gelen verileri oku (Boşaltma amaçlı)
                    if (stream.DataAvailable)
                    {
                        byte[] buffer = new byte[1024];
                        await stream.ReadAsync(buffer, 0, buffer.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bot {botId}] Hata: {ex.Message}");
            }
        }

        static async Task SendLogin(NetworkStream stream)
        {
            using ByteBuffer buffer = new ByteBuffer();
            buffer.WriteShort((short)MessageType.AuthLoginRequest);
            buffer.WriteString("1.0.0"); // ClientVersion
            buffer.WriteString("");      // Token (Boş = Yeni hesap)
            buffer.WriteString("");      // AccountID (Boş)
            buffer.WriteString("tr");    // Dil

            await SendPacket(stream, buffer);
        }

        static async Task SendPing(NetworkStream stream)
        {
            using ByteBuffer buffer = new ByteBuffer();
            buffer.WriteShort((short)MessageType.Ping);
            await SendPacket(stream, buffer);
        }

        static async Task SendMatchmaking(NetworkStream stream)
        {
            using ByteBuffer buffer = new ByteBuffer();
            buffer.WriteShort((short)MessageType.MatchMakingRequest);
            await SendPacket(stream, buffer);
        }

        static async Task SendCreateTeam(NetworkStream stream)
        {
            using ByteBuffer buffer = new ByteBuffer();
            buffer.WriteShort((short)MessageType.CreateTeamRequest);
            await SendPacket(stream, buffer);
        }

        static async Task SendClubMessage(NetworkStream stream, int botId)
        {
            using ByteBuffer buffer = new ByteBuffer();
            buffer.WriteShort((short)MessageType.SendClubMessage);
            buffer.WriteString($"Bot {botId} mesajı: {DateTime.Now}");
            await SendPacket(stream, buffer);
        }

        static async Task SendPacket(NetworkStream stream, ByteBuffer buffer)
        {
            byte[] data = buffer.ToArray();
            byte[] sizeHeader = BitConverter.GetBytes(data.Length);
            
            // Sunucu önce size(Int) bekliyor, sonra veriyi.
            await stream.WriteAsync(sizeHeader, 0, sizeHeader.Length);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }
    }
}
