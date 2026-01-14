using Discord;
using Discord.WebSocket;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Management;

public static class SystemInfoCommand
{
    public static async Task ShowSystemInfoAsync(SocketMessageComponent component)
    {
        try
        {
            // Embed oluştur
            var embed = CreateSystemInfoEmbed();
            
            // Butonları oluştur
            var components = new ComponentBuilder()
                .WithButton(new ButtonBuilder()
                    .WithLabel("💾 Detaylı RAM")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithCustomId("show_ram_details"))
                .WithButton(new ButtonBuilder()
                    .WithLabel("⚡ CPU Detay")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithCustomId("show_cpu_details"))
                .Build();

            await component.UpdateAsync(msg =>
            {
                msg.Embed = embed;
                msg.Components = components;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ System info error: {ex.Message}");
            await component.RespondAsync("Sistem bilgileri yüklenirken hata oluştu!", ephemeral: true);
        }
    }

    private static Embed CreateSystemInfoEmbed()
    {
        var process = Process.GetCurrentProcess();
        var startTime = process.StartTime;
        var uptime = DateTime.Now - startTime;

        // Sistem bilgilerini topla
        var cpuUsage = GetCpuUsage();
        var ramInfo = GetMemoryInfo();
        var diskInfo = GetDiskInfo();
        var networkInfo = GetNetworkInfo();
        var osInfo = GetOSInfo();

        var embedBuilder = new EmbedBuilder()
            .WithTitle("💻 Sistem Bilgileri")
            .WithColor(Color.Blue)
            .WithThumbnailUrl("https://cdn-icons-png.flaticon.com/512/3097/3097139.png")
            .WithTimestamp(DateTimeOffset.Now);

        // CPU Bilgileri
        embedBuilder.AddField("🖥️ CPU Kullanımı", 
            $"{cpuUsage}%\n" +
            $"Çekirdek: {Environment.ProcessorCount}\n" +
            $"64-bit: {(Environment.Is64BitOperatingSystem ? "Evet" : "Hayır")}",
            true);

        // RAM Bilgileri
        embedBuilder.AddField("🧠 Bellek (RAM)",
            $"{ramInfo.UsedMB:F1} MB / {ramInfo.TotalMB:F1} MB\n" +
            $"%{ramInfo.UsagePercent:F1} kullanım\n" +
            $"Process: {process.WorkingSet64 / 1024 / 1024} MB",
            true);

        // Disk Bilgileri
        embedBuilder.AddField("💾 Disk",
            $"{diskInfo.UsedGB:F1} GB / {diskInfo.TotalGB:F1} GB\n" +
            $"%{diskInfo.UsagePercent:F1} kullanım\n" +
            $"Boş: {diskInfo.FreeGB:F1} GB",
            true);

        // Sistem Bilgileri
        embedBuilder.AddField("⚙️ Sistem",
            $"OS: {osInfo.Name}\n" +
            $"Sürüm: {osInfo.Version}\n" +
            $"Makine: {Environment.MachineName}",
            false);

        // Ağ Bilgileri
        embedBuilder.AddField("🌐 Ağ",
            $"Ping: {networkInfo.Ping}ms\n" +
            $"Bağlantı: {networkInfo.Status}\n" +
            $"IP: {networkInfo.IPAddress}",
            true);

        // Çalışma Süresi
        embedBuilder.AddField("⏱️ Çalışma Süresi",
            $"{uptime.Days}g {uptime.Hours}sa {uptime.Minutes}dk\n" +
            $"Başlangıç: {startTime:HH:mm}\n" +
            $"PID: {process.Id}",
            true);

        // Performans Durumu
        var status = GetSystemStatus(cpuUsage, ramInfo.UsagePercent);
        embedBuilder.AddField("📈 Durum", status, false);

        return embedBuilder.Build();
    }

    private static float GetCpuUsage()
    {
        try
        {
            using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue(); // İlk değeri atla
            Thread.Sleep(500); // 0.5 saniye bekle
            return cpuCounter.NextValue();
        }
        catch
        {
            return 0;
        }
    }

    private static (float TotalMB, float UsedMB, float UsagePercent) GetMemoryInfo()
    {
        try
        {
            using var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            var availableMB = ramCounter.NextValue();
            var totalMB = GetTotalMemoryMB();
            var usedMB = totalMB - availableMB;
            var usagePercent = (usedMB / totalMB) * 100;

            return (totalMB, usedMB, usagePercent);
        }
        catch
        {
            return (0, 0, 0);
        }
    }

    private static float GetTotalMemoryMB()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var totalKB = Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
                return totalKB / 1024.0f;
            }
        }
        catch { }
        return 0;
    }

    private static (float TotalGB, float UsedGB, float FreeGB, float UsagePercent) GetDiskInfo()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:");
            var totalGB = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
            var freeGB = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
            var usedGB = totalGB - freeGB;
            var usagePercent = (usedGB / totalGB) * 100;

            return ((float)totalGB, (float)usedGB, (float)freeGB, (float)usagePercent);
        }
        catch
        {
            return (0, 0, 0, 0);
        }
    }

    private static (string Status, int Ping, string IPAddress) GetNetworkInfo()
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var activeInterface = networkInterfaces.FirstOrDefault(n => 
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            if (activeInterface != null)
            {
                var ipAddress = activeInterface.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.Address.ToString() ?? "Bilinmiyor";

                // Basit ping testi (localhost)
                using var ping = new Ping();
                var reply = ping.Send("8.8.8.8", 1000); // Google DNS
                var pingTime = reply?.RoundtripTime ?? 0;

                return ("Bağlı", (int)pingTime, ipAddress);
            }
        }
        catch { }

        return ("Bağlı Değil", 0, "Bilinmiyor");
    }

    private static (string Name, string Version) GetOSInfo()
    {
        try
        {
            var osName = Environment.OSVersion.VersionString;
            var osVersion = Environment.OSVersion.Version.ToString();
            
            // Daha okunabilir Windows sürümü
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                osName = "Windows";
                if (Environment.OSVersion.Version.Major == 10)
                    osName += " 10/11";
                else if (Environment.OSVersion.Version.Major == 6)
                    osName += " 7/8";
            }

            return (osName, osVersion);
        }
        catch
        {
            return ("Bilinmiyor", "Bilinmiyor");
        }
    }

    private static string GetSystemStatus(float cpuUsage, float ramUsage)
    {
        if (cpuUsage > 90 || ramUsage > 90)
            return "🔴 **KRİTİK** - Yüksek kaynak kullanımı!";
        else if (cpuUsage > 70 || ramUsage > 70)
            return "🟡 **UYARI** - Orta seviye yük";
        else if (cpuUsage > 50 || ramUsage > 50)
            return "🟠 **DİKKAT** - Artan yük";
        else
            return "🟢 **STABİL** - Sistem normal çalışıyor";
    }

    // Detaylı RAM bilgileri için
    public static async Task ShowRamDetailsAsync(SocketMessageComponent component)
    {
        var ramInfo = GetMemoryInfo();
        var process = Process.GetCurrentProcess();
        var processes = Process.GetProcesses();

        var embed = new EmbedBuilder()
            .WithTitle("🧠 Detaylı RAM Bilgileri")
            .WithColor(Color.Purple)
            .AddField("Toplam RAM", $"{ramInfo.TotalMB:F1} MB", true)
            .AddField("Kullanılan", $"{ramInfo.UsedMB:F1} MB", true)
            .AddField("Kullanım %", $"%{ramInfo.UsagePercent:F1}", true)
            .AddField("Bot Process", $"{process.WorkingSet64 / 1024 / 1024} MB", false)
            .AddField("Toplam Process", $"{processes.Length} adet", true)
            .AddField("En Çok Kullanan", GetTopMemoryProcess(), false)
            .WithFooter("RAM Monitör")
            .Build();

        await component.UpdateAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = CreateDetailButtons();
        });
    }

    // CPU detayları için
    public static async Task ShowCpuDetailsAsync(SocketMessageComponent component)
    {
        var cpuUsage = GetCpuUsage();

        var embed = new EmbedBuilder()
            .WithTitle("🖥️ CPU Detayları")
            .WithColor(Color.Green)
            .AddField("Anlık Kullanım", $"{cpuUsage:F1}%", true)
            .AddField("Çekirdek Sayısı", Environment.ProcessorCount.ToString(), true)
            .AddField("Mimari", Environment.Is64BitProcess ? "x64" : "x86", true)
            .AddField("İşlemci", GetProcessorName(), false)
            .AddField("En Çok CPU Kullanan", GetTopCpuProcess(), false)
            .AddField("Sistem Yükü", GetSystemLoadStatus(cpuUsage), false)
            .Build();

        await component.UpdateAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = CreateDetailButtons();
        });
    }

    private static string GetProcessorName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["Name"]?.ToString() ?? "Bilinmiyor";
            }
        }
        catch { }
        return "Bilinmiyor";
    }

    private static string GetTopMemoryProcess()
    {
        try
        {
            var process = Process.GetProcesses()
                .OrderByDescending(p => p.WorkingSet64)
                .FirstOrDefault();

            return process != null 
                ? $"{process.ProcessName}: {process.WorkingSet64 / 1024 / 1024} MB"
                : "Bilinmiyor";
        }
        catch
        {
            return "Bilinmiyor";
        }
    }

    private static string GetTopCpuProcess()
    {
        try
        {
            var process = Process.GetProcesses()
                .OrderByDescending(p => 
                {
                    try { return p.TotalProcessorTime; }
                    catch { return TimeSpan.Zero; }
                })
                .FirstOrDefault();

            return process != null 
                ? $"{process.ProcessName}: {process.TotalProcessorTime:mm\\:ss}"
                : "Bilinmiyor";
        }
        catch
        {
            return "Bilinmiyor";
        }
    }

    private static string GetSystemLoadStatus(float cpuUsage)
    {
        return cpuUsage switch
        {
            > 90 => "🔴 Çok Yüksek",
            > 70 => "🟠 Yüksek",
            > 50 => "🟡 Orta",
            > 30 => "🔵 Normal",
            _ => "🟢 Düşük"
        };
    }

    private static MessageComponent CreateDetailButtons()
    {
        return new ComponentBuilder()
            .WithButton(new ButtonBuilder()
                .WithLabel("↩️ Geri")
                .WithStyle(ButtonStyle.Secondary)
                .WithCustomId("show_system_info"))
            .WithButton(new ButtonBuilder()
                .WithLabel("🔄 Yenile")
                .WithStyle(ButtonStyle.Primary)
                .WithCustomId("refresh_system_info"))
            .WithButton(new ButtonBuilder()
                .WithLabel("📊 Ana Menü")
                .WithStyle(ButtonStyle.Success)
                .WithCustomId("back_to_stats"))
            .Build();
    }
}