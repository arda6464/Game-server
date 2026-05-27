using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace StressTester
{
    // Stres Testi Yapılandırması
    public static class StressTesterConfig
    {
        public static string TargetIP = "127.0.0.1";
        public static int TargetPort = 5000;
        public static int WebPort = 15000;
        public static int MaxBotCount = 200;
        public static int RampUpDelayMs = 25; // Botlar arası bağlantı açma gecikmesi
        public static int UdpInputIntervalMs = 100; // Savaş girdi paketi sıklığı (ms)
    }

    // Bot Durumları
    public enum BotState
    {
        Connecting,
        Authenticating,
        ConnectedLobby,
        InMatchmaking,
        InBattle,
        Disconnected
    }

    // UDP Mesaj Tipleri
    public enum UdpMessageType : byte
    {
        Connect = 0,
        ConnectResponse = 1,
        Move = 2,
        Shoot = 3,
        Input = 4,
        Ping = 5,
        Pong = 6,
    }

    // Canlı Telemetri İstatistikleri (Thread-Safe Atomik Sayaçlar)
    public static class TelemetryStats
    {
        public static long ActiveTcpBots = 0;
        public static long ActiveUdpBots = 0;
        public static long TargetBots = 0;
        public static long FailedBots = 0;

        public static long TcpPacketsSent = 0;
        public static long TcpPacketsRecv = 0;
        public static long UdpPacketsSent = 0;
        public static long UdpPacketsRecv = 0;

        public static long BytesSent = 0;
        public static long BytesRecv = 0;

        public static long TotalUdpPingRtt = 0;
        public static long UdpPingCount = 0;

        // Bant Genişliği Hesaplama Yardımcıları
        private static long lastBytesSent = 0;
        private static long lastBytesRecv = 0;
        public static double UploadSpeedMBs = 0;
        public static double DownloadSpeedMBs = 0;

        public static void CalculateSpeeds(double intervalSeconds)
        {
            long currentSent = Interlocked.Read(ref BytesSent);
            long currentRecv = Interlocked.Read(ref BytesRecv);

            UploadSpeedMBs = ((currentSent - lastBytesSent) / 1024.0 / 1024.0) / intervalSeconds;
            DownloadSpeedMBs = ((currentRecv - lastBytesRecv) / 1024.0 / 1024.0) / intervalSeconds;

            lastBytesSent = currentSent;
            lastBytesRecv = currentRecv;
        }

        public static void Reset()
        {
            ActiveTcpBots = 0;
            ActiveUdpBots = 0;
            FailedBots = 0;
            TcpPacketsSent = 0;
            TcpPacketsRecv = 0;
            UdpPacketsSent = 0;
            UdpPacketsRecv = 0;
            BytesSent = 0;
            BytesRecv = 0;
            TotalUdpPingRtt = 0;
            UdpPingCount = 0;
            lastBytesSent = 0;
            lastBytesRecv = 0;
            UploadSpeedMBs = 0;
            DownloadSpeedMBs = 0;
        }
    }

    class Program
    {
        private static CancellationTokenSource? _globalCts;
        private static readonly ConcurrentBag<StressBot> _activeBots = new ConcurrentBag<StressBot>();
        private static readonly ConcurrentQueue<string> _consoleLogs = new ConcurrentQueue<string>();
        private static readonly List<WebSocket> _webSockets = new List<WebSocket>();
        private static readonly object _wsLock = new object();

        static async Task Main(string[] args)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("=================================================");
            Console.WriteLine("     ANTIGRAVITY GAME SERVER STRES TESTER         ");
            Console.WriteLine("=================================================");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[Sistem] Web Arayüzü adresi: http://localhost:{StressTesterConfig.WebPort}");
            Console.ResetColor();

            // Arka planda HTTP & WebSocket Sunucusunu Başlat
            _ = Task.Run(() => StartWebServerAsync(StressTesterConfig.WebPort));

            // Telemetri Bant Genişliği Hesaplama Zamanlayıcısı
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    TelemetryStats.CalculateSpeeds(1.0);
                    await BroadcastTelemetryAsync();
                }
            });

            // Programın Kapanmasını Engelle
            await Task.Delay(-1);
        }

        public static void LogToWeb(string message, bool isError = false)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string formatted = $"[{time}] {message}";
            
            // Console'a da yazdır
            if (isError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(formatted);
            }
            else if (message.Contains("successfully") || message.Contains("Connected"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(formatted);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(formatted);
            }
            Console.ResetColor();

            _consoleLogs.Enqueue(JsonSerializer.Serialize(new { time, msg = message, err = isError }));
            while (_consoleLogs.Count > 100)
            {
                _consoleLogs.TryDequeue(out _);
            }
        }

        public static async Task StartStressTestAsync(string ip, int port, int botCount)
        {
            StopStressTest();

            StressTesterConfig.TargetIP = ip;
            StressTesterConfig.TargetPort = port;
            StressTesterConfig.MaxBotCount = botCount;

            TelemetryStats.Reset();
            TelemetryStats.TargetBots = botCount;
            _globalCts = new CancellationTokenSource();

            LogToWeb($"Stres testi başlatılıyor... Hedef: {ip}:{port} | Bot Sayısı: {botCount}", false);

            for (int i = 0; i < botCount; i++)
            {
                if (_globalCts.IsCancellationRequested) break;

                int botId = i + 1;
                var bot = new StressBot(botId, ip, port, _globalCts.Token);
                _activeBots.Add(bot);
                _ = Task.Run(() => bot.RunAsync());

                await Task.Delay(StressTesterConfig.RampUpDelayMs);
            }
        }

        public static void StopStressTest()
        {
            if (_globalCts != null)
            {
                _globalCts.Cancel();
                _globalCts.Dispose();
                _globalCts = null;
            }

            while (_activeBots.TryTake(out var bot))
            {
                bot.Dispose();
            }

            TelemetryStats.Reset();
            TelemetryStats.TargetBots = 0;
            LogToWeb("Stres testi durduruldu. Tüm bot bağlantıları kesildi.", false);
        }

        #region HTTP & WebSocket Server

        private static async Task StartWebServerAsync(int port)
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();

            while (true)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(() => HandleWebRequestAsync(context));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Web] Sunucu hatası: {ex.Message}");
                    break;
                }
            }
        }

        private static async Task HandleWebRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                if (request.IsWebSocketRequest)
                {
                    var wsContext = await context.AcceptWebSocketAsync(null);
                    await HandleWebSocketAsync(wsContext.WebSocket);
                    return;
                }

                if (request.Url?.AbsolutePath == "/" || request.Url?.AbsolutePath == "/index.html")
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(HtmlContent);
                    response.ContentType = "text/html; charset=utf-8";
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                else
                {
                    response.StatusCode = 404;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Web] İstek işleme hatası: {ex.Message}");
            }
            finally
            {
                response.Close();
            }
        }

        private static async Task HandleWebSocketAsync(WebSocket ws)
        {
            lock (_wsLock)
            {
                _webSockets.Add(ws);
            }

            LogToWeb("Yeni Web Kontrol Paneli bağlandı.", false);

            byte[] buffer = new byte[2048];

            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    try
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
                        if (data != null && data.TryGetValue("cmd", out var cmdObj))
                        {
                            string cmd = cmdObj.ToString() ?? "";
                            if (cmd == "start")
                            {
                                string targetIp = data["ip"].ToString() ?? "127.0.0.1";
                                int targetPort = int.Parse(data["port"].ToString() ?? "5000");
                                int botCount = int.Parse(data["botCount"].ToString() ?? "200");
                                _ = StartStressTestAsync(targetIp, targetPort, botCount);
                            }
                            else if (cmd == "stop")
                            {
                                StopStressTest();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToWeb($"WS Komut hatası: {ex.Message}", true);
                    }
                }
            }
            catch
            {
                // Bağlantı kesildi
            }
            finally
            {
                lock (_wsLock)
                {
                    _webSockets.Remove(ws);
                }
                ws.Dispose();
            }
        }

        private static async Task BroadcastTelemetryAsync()
        {
            WebSocket[] targets;
            lock (_wsLock)
            {
                targets = _webSockets.ToArray();
            }

            if (targets.Length == 0) return;

            long pingCount = Interlocked.Read(ref TelemetryStats.UdpPingCount);
            long pingSum = Interlocked.Read(ref TelemetryStats.TotalUdpPingRtt);
            int avgRtt = pingCount > 0 ? (int)(pingSum / pingCount) : 0;

            var telemetryData = new
            {
                targetBots = TelemetryStats.TargetBots,
                activeTcp = TelemetryStats.ActiveTcpBots,
                activeUdp = TelemetryStats.ActiveUdpBots,
                failed = TelemetryStats.FailedBots,
                tcpTx = Interlocked.Read(ref TelemetryStats.TcpPacketsSent),
                tcpRx = Interlocked.Read(ref TelemetryStats.TcpPacketsRecv),
                udpTx = Interlocked.Read(ref TelemetryStats.UdpPacketsSent),
                udpRx = Interlocked.Read(ref TelemetryStats.UdpPacketsRecv),
                upSpeed = TelemetryStats.UploadSpeedMBs,
                downSpeed = TelemetryStats.DownloadSpeedMBs,
                rtt = avgRtt,
                logs = _consoleLogs.ToArray()
            };

            string json = JsonSerializer.Serialize(telemetryData);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            foreach (var ws in targets)
            {
                if (ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch
                    {
                        // Hatalı soket
                    }
                }
            }
        }

        #endregion

        #region Beautiful Glassmorphism HTML/CSS/JS Panel Content

        private static readonly string HtmlContent = @"<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Antigravity - Stres Testi Paneli</title>
    <link href=""https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;600;700&display=swap"" rel=""stylesheet"">
    <style>
        :root {
            --bg-color: #080914;
            --card-bg: rgba(18, 20, 38, 0.45);
            --card-border: rgba(138, 43, 226, 0.25);
            --text-primary: #f0f1fa;
            --text-secondary: #a0a5c4;
            --accent-purple: #9d4edd;
            --accent-cyan: #00f5d4;
            --accent-pink: #ff007f;
            --glow-color: rgba(157, 78, 221, 0.35);
            --status-green: #00ff87;
            --status-red: #ff3838;
            --status-orange: #ffb300;
        }

        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: 'Outfit', sans-serif;
        }

        body {
            background-color: var(--bg-color);
            color: var(--text-primary);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            overflow-x: hidden;
            background-image: 
                radial-gradient(circle at 10% 20%, rgba(138, 43, 226, 0.15) 0%, transparent 40%),
                radial-gradient(circle at 90% 80%, rgba(0, 245, 212, 0.1) 0%, transparent 40%);
        }

        header {
            padding: 1rem 1.5rem;
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-bottom: 1px solid rgba(255, 255, 255, 0.05);
            backdrop-filter: blur(12px);
            position: sticky;
            top: 0;
            z-index: 100;
            background: rgba(8, 9, 20, 0.8);
        }

        .logo {
            display: flex;
            align-items: center;
            gap: 0.75rem;
            font-size: 1.3rem;
            font-weight: 700;
            letter-spacing: 0.5px;
            background: linear-gradient(45deg, var(--accent-purple), var(--accent-cyan));
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }

        .logo-ring {
            width: 16px;
            height: 16px;
            border-radius: 50%;
            border: 2.5px solid var(--accent-purple);
            box-shadow: 0 0 10px var(--accent-purple);
            animation: pulse-ring 2s infinite;
        }

        @keyframes pulse-ring {
            0% { transform: scale(1); opacity: 1; }
            50% { transform: scale(1.15); opacity: 0.7; box-shadow: 0 0 15px var(--accent-cyan); }
            100% { transform: scale(1); opacity: 1; }
        }

        .status-badge {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            font-size: 0.75rem;
            padding: 0.4rem 1rem;
            border-radius: 50px;
            background: rgba(255, 255, 255, 0.03);
            border: 1px solid rgba(255, 255, 255, 0.08);
            font-weight: 600;
        }

        .status-dot {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background-color: var(--status-red);
            box-shadow: 0 0 8px var(--status-red);
        }

        .status-dot.active {
            background-color: var(--status-green);
            box-shadow: 0 0 10px var(--status-green);
            animation: pulse-dot 1.5s infinite;
        }

        @keyframes pulse-dot {
            0% { opacity: 0.4; }
            50% { opacity: 1; }
            100% { opacity: 0.4; }
        }

        main {
            padding: 1.5rem;
            max-width: 1200px;
            margin: 0 auto;
            width: 100%;
            flex-grow: 1;
            display: grid;
            grid-template-columns: 300px 1fr;
            gap: 1.5rem;
        }

        /* Sayfa son derece küçüldüğünde veya daraltıldığında dikey sıralamaya geç */
        @media (max-width: 850px) {
            main {
                grid-template-columns: 1fr;
            }
        }

        .card {
            background: var(--card-bg);
            border: 1px solid var(--card-border);
            border-radius: 16px;
            backdrop-filter: blur(12px);
            padding: 1.25rem;
            box-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.25);
            display: flex;
            flex-direction: column;
            gap: 1.2rem;
            position: relative;
            overflow: hidden;
        }

        .card::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 3px;
            background: linear-gradient(90deg, var(--accent-purple), transparent);
        }

        .control-panel h2, .stats-panel h2 {
            font-size: 0.95rem;
            font-weight: 700;
            letter-spacing: 0.5px;
            color: var(--text-primary);
            border-bottom: 1px solid rgba(255, 255, 255, 0.05);
            padding-bottom: 0.5rem;
            text-transform: uppercase;
        }

        .input-group {
            display: flex;
            flex-direction: column;
            gap: 0.4rem;
        }

        label {
            font-size: 0.7rem;
            color: var(--text-secondary);
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        input[type=""text""], input[type=""number""] {
            background: rgba(255, 255, 255, 0.04);
            border: 1px solid rgba(255, 255, 255, 0.1);
            color: #fff;
            padding: 0.6rem 0.8rem;
            border-radius: 8px;
            font-size: 0.85rem;
            width: 100%;
            transition: all 0.2s ease;
        }

        input[type=""text""]:focus, input[type=""number""]:focus {
            outline: none;
            border-color: var(--accent-purple);
            box-shadow: 0 0 10px var(--glow-color);
            background: rgba(255, 255, 255, 0.08);
        }

        .slider-container {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 0.75rem;
        }

        input[type=""range""] {
            flex-grow: 1;
            accent-color: var(--accent-purple);
            cursor: pointer;
            height: 6px;
            border-radius: 5px;
        }

        .slider-val {
            font-weight: 700;
            color: var(--accent-cyan);
            font-size: 1rem;
            min-width: 40px;
            text-align: right;
        }

        .btn {
            padding: 0.75rem;
            border: none;
            border-radius: 8px;
            font-weight: 700;
            font-size: 0.85rem;
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 0.5rem;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            transition: all 0.2s cubic-bezier(0.175, 0.885, 0.32, 1.275);
        }

        .btn-start {
            background: linear-gradient(135deg, #00f5d4, #00bbf9);
            color: #080914;
            box-shadow: 0 4px 15px rgba(0, 245, 212, 0.2);
        }

        .btn-start:hover {
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(0, 245, 212, 0.4);
        }

        .btn-stop {
            background: linear-gradient(135deg, #ff007f, #7b2cbf);
            color: #fff;
            box-shadow: 0 4px 15px rgba(255, 0, 127, 0.2);
        }

        .btn-stop:hover {
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(255, 0, 127, 0.4);
        }

        .btn:disabled {
            opacity: 0.4;
            cursor: not-allowed;
            transform: none !important;
            box-shadow: none !important;
        }

        /* Right Dashboard Grid */
        .dashboard-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
            gap: 1rem;
        }

        .stat-card {
            background: rgba(255, 255, 255, 0.02);
            border: 1px solid rgba(255, 255, 255, 0.04);
            border-radius: 12px;
            padding: 1rem;
            display: flex;
            flex-direction: column;
            gap: 0.25rem;
            position: relative;
            overflow: hidden;
            transition: transform 0.2s ease, background-color 0.2s ease;
        }

        .stat-card:hover {
            transform: translateY(-2px);
            background: rgba(255, 255, 255, 0.04);
        }

        .stat-card::after {
            content: '';
            position: absolute;
            bottom: 0;
            left: 0;
            width: 100%;
            height: 2px;
            background: transparent;
        }

        .stat-card.active-bots::after { background: var(--accent-purple); }
        .stat-card.active-udp::after { background: var(--accent-cyan); }
        .stat-card.latency::after { background: var(--accent-pink); }
        .stat-card.bandwidth::after { background: #ffbe0b; }

        .stat-val {
            font-size: 1.6rem;
            font-weight: 700;
            letter-spacing: -0.5px;
            color: #fff;
        }

        .stat-lbl {
            font-size: 0.65rem;
            color: var(--text-secondary);
            text-transform: uppercase;
            font-weight: 600;
            letter-spacing: 0.5px;
        }

        .stat-detail {
            font-size: 0.7rem;
            color: var(--text-secondary);
            display: flex;
            justify-content: space-between;
            margin-top: 0.2rem;
        }

        .chart-container {
            flex-grow: 1;
            min-height: 180px;
            position: relative;
            background: rgba(255, 255, 255, 0.01);
            border: 1px solid rgba(255, 255, 255, 0.04);
            border-radius: 12px;
            padding: 0.5rem;
            overflow: hidden;
        }

        canvas {
            display: block;
            width: 100%;
            height: 100%;
        }

        .console-panel {
            background: rgba(4, 4, 10, 0.95);
            border: 1px solid rgba(138, 43, 226, 0.2);
            border-radius: 12px;
            padding: 0.75rem;
            font-family: monospace;
            font-size: 0.75rem;
            height: 140px;
            overflow-y: auto;
            color: #a0ffbe;
            display: flex;
            flex-direction: column;
            gap: 0.2rem;
            box-shadow: inset 0 0 10px rgba(0,0,0,0.8);
        }

        .console-line {
            display: flex;
            gap: 0.5rem;
            line-height: 1.1rem;
        }

        .console-time {
            color: #8b2bfe;
            font-weight: 600;
        }

        .console-text {
            color: #f0f1fa;
            word-break: break-all;
        }

        .console-text.error {
            color: var(--status-red);
        }

        .console-text.success {
            color: var(--status-green);
        }
    </style>
</head>
<body>
    <header>
        <div class=""logo"">
            <div class=""logo-ring""></div>
            <span>ANTIGRAVITY STRES TESTER</span>
        </div>
        <div class=""status-badge"">
            <div class=""status-dot"" id=""status-dot""></div>
            <span id=""status-text"">PASİF</span>
        </div>
    </header>

    <main>
        <!-- Sol Kontrol Paneli -->
        <div class=""card control-panel"">
            <h2>KONTROL PANELI</h2>
            
            <div class=""input-group"">
                <label>Hedef Sunucu IP</label>
                <input type=""text"" id=""target-ip"" value=""127.0.0.1"">
            </div>

            <div class=""input-group"">
                <label>Bağlantı Portu</label>
                <input type=""number"" id=""target-port"" value=""5000"">
            </div>

            <div class=""input-group"">
                <label>Bot Sayısı</label>
                <div class=""slider-container"">
                    <input type=""range"" id=""bot-slider"" min=""1"" max=""1000"" value=""200"">
                    <span class=""slider-val"" id=""slider-val"">200</span>
                </div>
            </div>

            <button class=""btn btn-start"" id=""btn-start"">Stres Testini Başlat</button>
            <button class=""btn btn-stop"" id=""btn-stop"" disabled>Testi Durdur</button>
        </div>

        <!-- Sağ Canlı Metrikler & Grafikler -->
        <div class=""card stats-panel"">
            <h2>CANLI METRIKLER</h2>

            <div class=""dashboard-grid"">
                <div class=""stat-card active-bots"">
                    <span class=""stat-val"" id=""lbl-tcp"">0</span>
                    <span class=""stat-lbl"">TCP AKTIF BOTLAR</span>
                    <div class=""stat-detail"">
                        <span>Hedef: <span id=""lbl-target"">0</span></span>
                        <span>Hata: <span id=""lbl-failed"" style=""color: var(--status-red)"">0</span></span>
                    </div>
                </div>

                <div class=""stat-card active-udp"">
                    <span class=""stat-val"" id=""lbl-udp"">0</span>
                    <span class=""stat-lbl"">UDP AKTIF SAVAŞ BOTLARI</span>
                    <div class=""stat-detail"">
                        <span>El Sıkışma: <span id=""lbl-udp-percent"">0%</span></span>
                    </div>
                </div>

                <div class=""stat-card latency"">
                    <span class=""stat-val"" id=""lbl-rtt"">0 ms</span>
                    <span class=""stat-lbl"">ORTALAMA GECIKME (RTT)</span>
                    <div class=""stat-detail"">
                        <span id=""lbl-rtt-status"">Bilinmiyor</span>
                    </div>
                </div>

                <div class=""stat-card bandwidth"">
                    <span class=""stat-val"" id=""lbl-bw"">0.0 MB/s</span>
                    <span class=""stat-lbl"">ANLIK TRAFIK</span>
                    <div class=""stat-detail"">
                        <span>Yükleme: <span id=""lbl-up"">0.0 MB/s</span></span>
                    </div>
                </div>
            </div>

            <!-- Canlı RTT Grafiği -->
            <div class=""chart-container"">
                <canvas id=""rtt-chart""></canvas>
            </div>

            <!-- Canlı Konsol Logları -->
            <div class=""console-panel"" id=""console-panel"">
                <div class=""console-line"">
                    <span class=""console-time"">Sistem:</span>
                    <span class=""console-text info"">Web paneli başarıyla yüklendi. Testi başlatmak için butona basın.</span>
                </div>
            </div>
        </div>
    </main>

    <script>
        const slider = document.getElementById('bot-slider');
        const sliderVal = document.getElementById('slider-val');
        const btnStart = document.getElementById('btn-start');
        const btnStop = document.getElementById('btn-stop');
        const statusDot = document.getElementById('status-dot');
        const statusText = document.getElementById('status-text');
        const consolePanel = document.getElementById('console-panel');

        slider.oninput = function() {
            sliderVal.innerText = this.value;
        };

        // WebSocket Kurulumu
        let ws;
        function connectWS() {
            ws = new WebSocket('ws://' + window.location.host);
            
            ws.onmessage = function(event) {
                const data = JSON.parse(event.data);
                
                // Metrikleri Güncelle
                document.getElementById('lbl-tcp').innerText = data.activeTcp;
                document.getElementById('lbl-target').innerText = data.targetBots;
                document.getElementById('lbl-failed').innerText = data.failed;
                document.getElementById('lbl-udp').innerText = data.activeUdp;
                
                const percent = data.activeTcp > 0 ? Math.round((data.activeUdp / data.activeTcp) * 100) : 0;
                document.getElementById('lbl-udp-percent').innerText = percent + '%';
                
                document.getElementById('lbl-rtt').innerText = data.rtt + ' ms';
                const rttStatus = document.getElementById('lbl-rtt-status');
                if (data.rtt === 0) {
                    rttStatus.innerText = 'Bilinmiyor';
                    rttStatus.style.color = 'var(--text-secondary)';
                } else if (data.rtt < 60) {
                    rttStatus.innerText = 'Mükemmel';
                    rttStatus.style.color = 'var(--status-green)';
                } else if (data.rtt < 150) {
                    rttStatus.innerText = 'Normal';
                    rttStatus.style.color = 'var(--status-orange)';
                } else {
                    rttStatus.innerText = 'Kötü';
                    rttStatus.style.color = 'var(--status-red)';
                }

                const totalBw = (data.upSpeed + data.downSpeed).toFixed(2);
                document.getElementById('lbl-bw').innerText = totalBw + ' MB/s';
                document.getElementById('lbl-up').innerText = data.upSpeed.toFixed(2) + ' MB/s';

                // Panel Durumu
                if (data.targetBots > 0) {
                    statusDot.className = 'status-dot active';
                    statusText.innerText = 'AKTIF';
                    btnStart.disabled = true;
                    btnStop.disabled = false;
                } else {
                    statusDot.className = 'status-dot';
                    statusText.innerText = 'PASİF';
                    btnStart.disabled = false;
                    btnStop.disabled = true;
                }

                // Konsol Logları
                if (data.logs && data.logs.length > 0) {
                    data.logs.forEach(logStr => {
                        try {
                            const log = JSON.parse(logStr);
                            addConsoleLine(log.time, log.msg, log.err);
                        } catch (e) {}
                    });
                }

                // Grafik Güncelle
                updateChart(data.rtt, data.activeTcp);
            };

            ws.onclose = function() {
                setTimeout(connectWS, 1000);
            };
        }

        function addConsoleLine(time, text, isError) {
            // Çift basımı engellemek için kontrol et
            const lines = consolePanel.children;
            if (lines.length > 0) {
                const lastLine = lines[lines.length - 1];
                if (lastLine.innerText.includes(text)) return;
            }

            const line = document.createElement('div');
            line.className = 'console-line';
            
            const timeSpan = document.createElement('span');
            timeSpan.className = 'console-time';
            timeSpan.innerText = '[' + time + ']';
            
            const textSpan = document.createElement('span');
            textSpan.className = 'console-text' + (isError ? ' error' : '');
            textSpan.innerText = text;
            
            line.appendChild(timeSpan);
            line.appendChild(textSpan);
            consolePanel.appendChild(line);
            consolePanel.scrollTop = consolePanel.scrollHeight;
        }

        btnStart.onclick = function() {
            const ip = document.getElementById('target-ip').value;
            const port = document.getElementById('target-port').value;
            const botCount = slider.value;

            ws.send(JSON.stringify({
                cmd: 'start',
                ip: ip,
                port: port,
                botCount: botCount
            }));
        };

        btnStop.onclick = function() {
            ws.send(JSON.stringify({
                cmd: 'stop'
            }));
        };

        // Canvas Grafik Çizimi (Dependency-free Lightweight Line Chart)
        const canvas = document.getElementById('rtt-chart');
        const ctx = canvas.getContext('2d');
        const rttHistory = [];
        const botHistory = [];
        const maxPoints = 30;

        function resizeCanvas() {
            canvas.width = canvas.parentElement.clientWidth;
            canvas.height = canvas.parentElement.clientHeight;
        }
        window.addEventListener('resize', resizeCanvas);
        resizeCanvas();

        function updateChart(rtt, botCount) {
            rttHistory.push(rtt);
            botHistory.push(botCount);
            if (rttHistory.length > maxPoints) {
                rttHistory.shift();
                botHistory.shift();
            }

            drawChart();
        }

        function drawChart() {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            if (rttHistory.length === 0) return;

            const padding = 20;
            const w = canvas.width - padding * 2;
            const h = canvas.height - padding * 2;

            // En büyük değerleri bul
            const maxRtt = Math.max(...rttHistory, 100);
            const maxBots = Math.max(...botHistory, 100);

            // Gecikme Grafiği (Pembe Çizgi)
            ctx.beginPath();
            ctx.strokeStyle = '#ff007f';
            ctx.lineWidth = 3;
            ctx.shadowBlur = 10;
            ctx.shadowColor = 'rgba(255, 0, 127, 0.5)';
            for (let i = 0; i < rttHistory.length; i++) {
                const x = padding + (i / (maxPoints - 1)) * w;
                const y = padding + h - (rttHistory[i] / maxRtt) * h;
                if (i === 0) ctx.moveTo(x, y);
                else ctx.lineTo(x, y);
            }
            ctx.stroke();

            // Bot Sayısı Grafiği (Mor Çizgi)
            ctx.beginPath();
            ctx.strokeStyle = '#9d4edd';
            ctx.lineWidth = 2.5;
            ctx.shadowBlur = 6;
            ctx.shadowColor = 'rgba(157, 78, 221, 0.4)';
            for (let i = 0; i < botHistory.length; i++) {
                const x = padding + (i / (maxPoints - 1)) * w;
                const y = padding + h - (botHistory[i] / maxBots) * h;
                if (i === 0) ctx.moveTo(x, y);
                else ctx.lineTo(x, y);
            }
            ctx.stroke();

            ctx.shadowBlur = 0; // Gölgeyi temizle
        }

        connectWS();
    </script>
</body>
</html>";

        #endregion
    }

    // Tek bir Asenkron Stres İstemcisi (Ajan)
    public class StressBot : IDisposable
    {
        public int Id { get; }
        public string TargetIP { get; }
        public int TargetPort { get; }
        public BotState State { get; set; } = BotState.Connecting;
        public int AccountId { get; set; }
        public string Token { get; set; } = "";
        public int ConnectionToken { get; set; } = 0;
        public int LastTcpPingRtt { get; set; } = 0;
        public bool IsRunning => !_cts.IsCancellationRequested;

        private readonly CancellationToken _parentToken;
        private CancellationTokenSource _cts;
        private TcpClient? _tcpClient;
        private NetworkStream? _tcpStream;
        private UdpClient? _udpClient;
        private int _udpSeqCounter = 0;

        public StressBot(int id, string ip, int port, CancellationToken parentToken)
        {
            Id = id;
            TargetIP = ip;
            TargetPort = port;
            _parentToken = parentToken;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        }

        public void Log(string message, bool isError = false)
        {
            if (Id == 1 || Id % 25 == 0 || isError)
            {
                Program.LogToWeb($"[Bot {Id}] {message}", isError);
            }
        }

        public async Task RunAsync()
        {
            try
            {
                // 1. TCP ile Oyun Sunucusuna Bağlan
                _tcpClient = new TcpClient();
                _tcpClient.NoDelay = true; // Nagle algoritmasını kapat
                await _tcpClient.ConnectAsync(TargetIP, TargetPort);
                _tcpStream = _tcpClient.GetStream();

                Interlocked.Increment(ref TelemetryStats.ActiveTcpBots);
                State = BotState.Authenticating;
                Log("Connected via TCP. Sending FirstConnection request...");

                // TCP Okuma Döngüsünü Arka Planda Başlat
                _ = Task.Run(() => StartTcpReadLoopAsync());

                // 2. İlk Bağlantı El Sıkışmasını Gönder (FirstConnectionRequest)
                await SendTcpFirstConnectionAsync();

                // 3. TCP Ping Döngüsü (Her 5 saniyede bir keep-alive)
                _ = Task.Run(() => StartTcpPingLoopAsync());

                // 4. Periyodik Eşleştirme ve Sohbet Simülasyonu
                _ = Task.Run(() => StartSimulatedActionsLoopAsync());
            }
            catch (Exception ex)
            {
                HandleFailure($"TCP Connection failed: {ex.Message}");
            }
        }

        private async Task SendTcpFirstConnectionAsync()
        {
            if (_tcpStream == null) return;

            using var buffer = new ByteBuffer();
            buffer.WriteVarInt((int)MessageType.FirstConnectionRequest);
            buffer.WriteVarString($"Bot-{Id}");      // DeviceName
            buffer.WriteVarString("PC-StressBot");   // DeviceModel
            buffer.WriteVarString("ARDA64");         // ClientKey

            byte[] data = buffer.ToArray();
            await _tcpStream.WriteAsync(data, 0, data.Length);
            await _tcpStream.FlushAsync();

            Interlocked.Increment(ref TelemetryStats.TcpPacketsSent);
            Interlocked.Add(ref TelemetryStats.BytesSent, data.Length);
        }

        private async Task SendTcpLoginAsync()
        {
            if (_tcpStream == null) return;

            using var buffer = new ByteBuffer();
            buffer.WriteVarInt((int)MessageType.AuthLoginRequest);
            buffer.WriteVarString("1.0"); // ServerVersion = "1.0" in config.json
            buffer.WriteVarString("");    // Token (Boş = Yeni hesap)
            buffer.WriteVarInt(0);        // AccountID (0 = Yeni hesap)
            buffer.WriteVarString("tr");  // Dil

            byte[] data = buffer.ToArray();
            await _tcpStream.WriteAsync(data, 0, data.Length);
            await _tcpStream.FlushAsync();

            Interlocked.Increment(ref TelemetryStats.TcpPacketsSent);
            Interlocked.Add(ref TelemetryStats.BytesSent, data.Length);
        }

        private async Task StartTcpReadLoopAsync()
        {
            byte[] readBuffer = new byte[8192];
            try
            {
                while (IsRunning && _tcpClient != null && _tcpClient.Connected && _tcpStream != null)
                {
                    int bytesRead = await _tcpStream.ReadAsync(readBuffer, 0, readBuffer.Length, _cts.Token);
                    if (bytesRead <= 0) break;

                    Interlocked.Add(ref TelemetryStats.BytesRecv, bytesRead);

                    // Gelen paketi ByteBuffer'a aktarıp ayrıştır
                    using var packet = new ByteBuffer(bytesRead);
                    packet.WriteBytes(readBuffer.AsSpan(0, bytesRead), true);

                    while (packet.Position < packet.Length)
                    {
                        try
                        {
                            int packetTypeVal = packet.ReadVarInt();
                            var type = (MessageType)packetTypeVal;

                            Interlocked.Increment(ref TelemetryStats.TcpPacketsRecv);

                            if (type == MessageType.FirstConnectionResponse)
                            {
                                bool success = packet.ReadBool();
                                string reason = packet.ReadVarString();
                                if (success)
                                {
                                    Log("First Connection Handshake successful! Authenticating...");
                                    await SendTcpLoginAsync();
                                }
                                else
                                {
                                    HandleFailure($"FirstConnection rejected: {reason}");
                                    break;
                                }
                            }
                            else if (type == MessageType.NewAccountCreateResponse)
                            {
                                string token = packet.ReadVarString();
                                int id = packet.ReadVarInt();
                                this.Token = token;
                                this.AccountId = id;
                                Log($"New account registered: ID {id}");
                            }
                            else if (type == MessageType.AuthLoginResponse)
                            {
                                int connToken = packet.ReadVarInt();
                                this.ConnectionToken = connToken;
                                this.State = BotState.ConnectedLobby;
                                Log($"Auth Successful. Token: {connToken}");

                                // UDP el sıkışmasını tetikle
                                _ = Task.Run(() => StartUdpHandshakeAsync());

                                // AuthLoginResponse is a very large packet. We must discard the remaining payload bytes
                                // of this packet in the current buffer so that they are not parsed as separate packet types.
                                break;
                            }
                            else if (type == MessageType.MatchMakingUpdate)
                            {
                                int pPerMatch = packet.ReadVarInt();
                                int curPlayers = packet.ReadVarInt();
                                Log($"Matchmaking update: {curPlayers}/{pPerMatch}");
                            }
                            else if (type == MessageType.MatchFound)
                            {
                                int playerCount = packet.ReadVarInt();
                                uint tick = packet.ReadUInt();
                                for (int i = 0; i < playerCount; i++)
                                {
                                    int pid = packet.ReadVarInt();
                                    string name = packet.ReadVarString();
                                    int health = packet.ReadVarInt();
                                    float x = packet.ReadFloat();
                                    float y = packet.ReadFloat();
                                    float z = packet.ReadFloat();
                                }
                                this.State = BotState.InBattle;
                                Log("Match Found! Transitioning to Battle...");
                                
                                // MatchFound can be large. Discard remaining payload.
                                break;
                            }
                            else if (type == MessageType.Pong)
                            {
                                float clientSentTime = packet.ReadFloat();
                                int rtt = (int)((GetTimeSeconds() - clientSentTime) * 1000);
                                this.LastTcpPingRtt = rtt;
                            }
                            else
                            {
                                // Diğer paketleri görmezden gel ve buffer'ı boşalt
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Packet parsing error: {ex.Message}", true);
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Okuma hatası veya iptal
            }
            finally
            {
                HandleFailure("TCP socket closed.");
            }
        }

        private async Task StartTcpPingLoopAsync()
        {
            try
            {
                while (IsRunning && _tcpClient != null && _tcpClient.Connected && _tcpStream != null)
                {
                    await Task.Delay(5000, _cts.Token);

                    using var buffer = new ByteBuffer();
                    buffer.WriteVarInt((int)MessageType.Ping);
                    buffer.WriteFloat((float)GetTimeSeconds());

                    byte[] data = buffer.ToArray();
                    await _tcpStream.WriteAsync(data, 0, data.Length, _cts.Token);
                    await _tcpStream.FlushAsync(_cts.Token);

                    Interlocked.Increment(ref TelemetryStats.TcpPacketsSent);
                    Interlocked.Add(ref TelemetryStats.BytesSent, data.Length);
                }
            }
            catch { }
        }

        public async Task StartUdpHandshakeAsync()
        {
            if (ConnectionToken == 0) return;

            try
            {
                _udpClient = new UdpClient();
                _udpClient.Connect(TargetIP, TargetPort);

                // UDP Okuma Döngüsünü Arka Planda Başlat
                _ = Task.Run(() => StartUdpReadLoopAsync());

                Log("Initiating UDP Connect Handshake...");

                // UDP Connect Paketini Gönder
                using var payload = new ByteBuffer();
                payload.WriteVarInt((int)UdpMessageType.Connect);

                await SendUdpUnreliableAsync(payload.ToArray());

                // Periyodik UDP Paket Gönderimi (Ping ve Savaş Girdileri)
                _ = Task.Run(() => StartUdpGameplayLoopAsync());
            }
            catch (Exception ex)
            {
                Log($"UDP Handshake failed: {ex.Message}", true);
            }
        }

        private async Task StartUdpReadLoopAsync()
        {
            if (_udpClient == null) return;

            try
            {
                while (IsRunning)
                {
                    var result = await _udpClient.ReceiveAsync(_parentToken);
                    Interlocked.Add(ref TelemetryStats.BytesRecv, result.Buffer.Length);

                    using var buffer = new ByteBuffer(result.Buffer.Length);
                    buffer.WriteBytes(result.Buffer, true);

                    // Header'ı Oku (Server'dan gelen paketlerde token bulunmaz)
                    byte flags = (byte)buffer.ReadVarInt();
                    int seqNo = buffer.ReadVarInt();

                    // Bu bir Reliable Paket ise ACKLamamız gerekir
                    if ((flags & 1) != 0) // Reliable flag = 1
                    {
                        await SendUdpAckAsync(seqNo);
                    }

                    // Payload Oku
                    byte[] payload = buffer.GetReadableSpan().ToArray();
                    if (payload.Length == 0) continue;

                    using var payloadBuffer = new ByteBuffer(payload.Length);
                    payloadBuffer.WriteBytes(payload, true);

                    byte udpMsgType = (byte)payloadBuffer.ReadVarInt();
                    Interlocked.Increment(ref TelemetryStats.UdpPacketsRecv);

                    if (udpMsgType == (byte)UdpMessageType.ConnectResponse)
                    {
                        State = BotState.InBattle;
                        Interlocked.Increment(ref TelemetryStats.ActiveUdpBots);
                        Log("UDP Connected successfully! Entering combat mode...");
                    }
                    else if (udpMsgType == (byte)UdpMessageType.Pong)
                    {
                        float clientSentTime = payloadBuffer.ReadFloat();
                        long rtt = (long)((GetTimeSeconds() - clientSentTime) * 1000);
                        if (rtt > 0 && rtt < 2000)
                        {
                            Interlocked.Add(ref TelemetryStats.TotalUdpPingRtt, rtt);
                            Interlocked.Increment(ref TelemetryStats.UdpPingCount);
                        }
                    }
                    else if (udpMsgType == (byte)UdpMessageType.Move)
                    {
                        uint clientTick = payloadBuffer.ReadUInt();
                        int pid = payloadBuffer.ReadVarInt();
                        float x = payloadBuffer.ReadFloat();
                        float y = payloadBuffer.ReadFloat();
                        float z = payloadBuffer.ReadFloat();
                    }
                    else if (udpMsgType == (byte)UdpMessageType.Shoot)
                    {
                        int ownerId = payloadBuffer.ReadVarInt();
                        float dirX = payloadBuffer.ReadFloat();
                        float dirY = payloadBuffer.ReadFloat();
                        int bulletId = payloadBuffer.ReadVarInt();
                    }
                }
            }
            catch { }
        }

        private async Task StartUdpGameplayLoopAsync()
        {
            if (_udpClient == null) return;

            int tick = 0;
            Random rnd = new Random();

            try
            {
                while (IsRunning)
                {
                    // Her 100ms'de bir hareket girdisi gönder (Savaş simülasyonu)
                    await Task.Delay(StressTesterConfig.UdpInputIntervalMs, _cts.Token);

                    if (State == BotState.InBattle)
                    {
                        using var payload = new ByteBuffer();
                        payload.WriteVarInt((int)UdpMessageType.Input);

                        // Rastgele Yönler
                        byte inputByte = 0;
                        int dir = rnd.Next(0, 5);
                        if (dir == 1) inputByte |= 1;       // X=1
                        else if (dir == 2) inputByte |= 2;  // X=-1
                        if (dir == 3) inputByte |= 4;       // Y=1
                        else if (dir == 4) inputByte |= 8;  // Y=-1

                        payload.WriteByte(inputByte);
                        payload.WriteVarInt(tick++);

                        await SendUdpUnreliableAsync(payload.ToArray());

                        // %10 ihtimalle ateş et (Her saniye ortalama 1 atış)
                        if (rnd.Next(0, 10) == 0)
                        {
                            using var shootPayload = new ByteBuffer();
                            shootPayload.WriteVarInt((int)UdpMessageType.Shoot);
                            shootPayload.WriteVarInt(AccountId);
                            
                            // Rastgele açı ile ateş et
                            double angle = rnd.NextDouble() * Math.PI * 2;
                            shootPayload.WriteFloat((float)Math.Cos(angle));
                            shootPayload.WriteFloat((float)Math.Sin(angle));
                            shootPayload.WriteVarInt(0); // BulletId

                            await SendUdpUnreliableAsync(shootPayload.ToArray());
                        }
                    }

                    // Her 2 saniyede bir UDP Ping
                    if (tick % 20 == 0)
                    {
                        using var pingPayload = new ByteBuffer();
                        pingPayload.WriteVarInt((int)UdpMessageType.Ping);
                        pingPayload.WriteFloat((float)GetTimeSeconds());

                        await SendUdpUnreliableAsync(pingPayload.ToArray());
                    }
                }
            }
            catch { }
        }

        private async Task SendUdpUnreliableAsync(byte[] payload)
        {
            if (_udpClient == null || ConnectionToken == 0) return;

            using var buffer = new ByteBuffer();
            buffer.WriteVarInt(0); // UdpPacketFlags.None = 0
            buffer.WriteVarInt(_udpSeqCounter++);
            buffer.WriteVarInt(ConnectionToken);
            buffer.WriteBytes(payload, false);

            byte[] data = buffer.ToArray();
            await _udpClient.SendAsync(data, data.Length);

            Interlocked.Increment(ref TelemetryStats.UdpPacketsSent);
            Interlocked.Add(ref TelemetryStats.BytesSent, data.Length);
        }

        private async Task SendUdpAckAsync(int receivedSeqNo)
        {
            if (_udpClient == null || ConnectionToken == 0) return;

            using var buffer = new ByteBuffer();
            buffer.WriteVarInt(2); // UdpPacketFlags.Ack = 2
            buffer.WriteVarInt(receivedSeqNo);
            buffer.WriteVarInt(ConnectionToken);

            byte[] data = buffer.ToArray();
            await _udpClient.SendAsync(data, data.Length);

            Interlocked.Increment(ref TelemetryStats.UdpPacketsSent);
            Interlocked.Add(ref TelemetryStats.BytesSent, data.Length);
        }

        private async Task StartSimulatedActionsLoopAsync()
        {
            Random rnd = new Random();
            try
            {
                while (IsRunning && _tcpClient != null && _tcpClient.Connected && _tcpStream != null)
                {
                    // Her 10 ila 15 saniyede bir rastgele TCP eylemi yap (Eşleştirme sırasına girme veya Kulüp sohbeti)
                    await Task.Delay(rnd.Next(10000, 15000), _cts.Token);

                    if (State == BotState.ConnectedLobby || State == BotState.InBattle)
                    {
                        int action = rnd.Next(0, 3);
                        if (action == 0)
                        {
                            // Eşleştirme Sırasına Gir
                            using var buffer = new ByteBuffer();
                            buffer.WriteVarInt((int)MessageType.MatchMakingRequest);

                            byte[] data = buffer.ToArray();
                            await _tcpStream.WriteAsync(data, 0, data.Length, _cts.Token);
                            await _tcpStream.FlushAsync(_cts.Token);
                            
                            Interlocked.Increment(ref TelemetryStats.TcpPacketsSent);
                            Interlocked.Add(ref TelemetryStats.BytesSent, data.Length);
                            Log("TCP: Entered Matchmaking queue.");
                        }
                        else if (action == 1)
                        {
                            // Kulüp Sohbetine Mesaj Gönder
                            using var buffer = new ByteBuffer();
                            buffer.WriteVarInt((int)MessageType.SendClubMessage);
                            buffer.WriteVarString($"Bot #{Id} selamlar! Lokasyon stress-test: {DateTime.Now:HH:mm:ss}");

                            byte[] data = buffer.ToArray();
                            await _tcpStream.WriteAsync(data, 0, data.Length, _cts.Token);
                            await _tcpStream.FlushAsync(_cts.Token);

                            Interlocked.Increment(ref TelemetryStats.TcpPacketsSent);
                            Interlocked.Add(ref TelemetryStats.BytesSent, data.Length);
                            Log("TCP: Sent Club chat message.");
                        }
                    }
                }
            }
            catch { }
        }

        private void HandleFailure(string reason)
        {
            if (State != BotState.Disconnected)
            {
                State = BotState.Disconnected;
                
                if (_tcpClient != null)
                {
                    Interlocked.Decrement(ref TelemetryStats.ActiveTcpBots);
                }
                if (_udpClient != null && Interlocked.Read(ref TelemetryStats.ActiveUdpBots) > 0)
                {
                    Interlocked.Decrement(ref TelemetryStats.ActiveUdpBots);
                }

                Interlocked.Increment(ref TelemetryStats.FailedBots);
                Log($"Bot disconnected. Reason: {reason}", true);
                
                Dispose();
            }
        }

        private double GetTimeSeconds()
        {
            return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _tcpStream?.Close(); } catch { }
            try { _tcpClient?.Close(); } catch { }
            try { _udpClient?.Close(); } catch { }
            _cts.Dispose();
        }
    }
}
