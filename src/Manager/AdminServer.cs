using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Threading.Tasks;

public class AdminServer
{
    private bool _isRunning;
    private string _adminPath;

    public class SimpleHttpRequest
    {
        public string Method { get; set; } = "GET";
        public string Path { get; set; } = "/";
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> Cookies { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> QueryString { get; set; } = new Dictionary<string, string>();
        public byte[] Body { get; set; } = Array.Empty<byte>();
        public string RemoteEndPoint { get; set; } = "";
    }

    public class SimpleHttpResponse
    {
        public int StatusCode { get; set; } = 200;
        public string ContentType { get; set; } = "text/html";
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public MemoryStream OutputStream { get; set; } = new MemoryStream();
        public List<string> CookiesToSet { get; set; } = new List<string>();

        public void Redirect(string url)
        {
            StatusCode = 302;
            Headers["Location"] = url;
        }

        public void AppendCookie(string name, string value, string path = "/", DateTime? expires = null, bool httpOnly = false)
        {
            string cookie = $"{name}={value}; Path={path}";
            if (expires.HasValue) cookie += $"; Expires={expires.Value:R}";
            if (httpOnly) cookie += "; HttpOnly";
            CookiesToSet.Add(cookie);
        }
    }

    public class SimpleHttpContext
    {
        public SimpleHttpRequest Request { get; set; } = new SimpleHttpRequest();
        public SimpleHttpResponse Response { get; set; } = new SimpleHttpResponse();
    }

    // Session management: Token -> (Username, Expiry)
    private static System.Collections.Concurrent.ConcurrentDictionary<string, (string Username, DateTime Expiry)> _sessions
        = new System.Collections.Concurrent.ConcurrentDictionary<string, (string, DateTime)>();

    // CPU Tracking
    private DateTime _lastCpuCheck = DateTime.MinValue;
    private TimeSpan _lastProcessorTime;
    private double _currentCpuUsage = 0;

    public AdminServer()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Define potential locations for the admin folder
        string[] potentialPaths = new string[]
        {
            Path.Combine(baseDir, "admin"), // Build output
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "admin")), // Development (from bin/Debug/netX)
            Path.Combine(Directory.GetCurrentDirectory(), "admin"), // Project root if run from there
            Path.Combine(Directory.GetCurrentDirectory(), "src", "admin"), // Legacy src location
            "C:\\Project\\Game-server\\admin" // Absolute fallback for this environment
        };

        _adminPath = string.Empty;
        foreach (var path in potentialPaths)
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "index.html")))
            {
                _adminPath = path;
                break;
            }
        }

        if (string.IsNullOrEmpty(_adminPath))
        {
            _adminPath = Path.Combine(baseDir, "admin");
            Directory.CreateDirectory(_adminPath);
            Console.WriteLine($"[AdminServer] UYARI: Admin klasörü bulunamadı, oluşturuldu: {_adminPath}");
            Logger.errorslog($"[AdminServer] UYARI: Admin klasörü bulunamadı, boş bir klasör oluşturuldu: {_adminPath}");
        }
        else
        {
            Console.WriteLine($"[AdminServer] Admin klasörü bulundu: {_adminPath}");
            Logger.genellog($"[AdminServer] Admin klasörü bulundu: {_adminPath}");
        }

        // Bağımsız admin hesap sistemini başlat
        AdminAccountManager.Initialize();
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        Logger.genellog("[AdminServer] Admin servisleri hazır.");
    }

    public void Stop()
    {
        _isRunning = false;
        Logger.genellog("[AdminServer] Admin paneli durduruldu.");
    }

    public void HandleConnection(TcpClient client, byte[] initialData)
    {
        Task.Run(async () =>
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    // İstek verisini oku
                    byte[] buffer = new byte[65536]; // Genişletilmiş buffer
                    int totalRead = initialData.Length;
                    Array.Copy(initialData, 0, buffer, 0, initialData.Length);

                    // Header bitene kadar oku
                    while (totalRead < buffer.Length && !Encoding.UTF8.GetString(buffer, 0, totalRead).Contains("\r\n\r\n"))
                    {
                        int read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead);
                        if (read <= 0) break;
                        totalRead += read;
                    }

                    // Content-Length kontrolü yap ve body'yi tam olarak al
                    string requestStr = Encoding.UTF8.GetString(buffer, 0, totalRead);
                    int bodyStartIdx = requestStr.IndexOf("\r\n\r\n");
                    if (bodyStartIdx != -1)
                    {
                        bodyStartIdx += 4; // "\r\n\r\n" uzunluğu
                        int contentLength = 0;
                        string[] lines = requestStr.Substring(0, bodyStartIdx).Split(new[] { "\r\n" }, StringSplitOptions.None);
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                            {
                                int.TryParse(line.Substring(15).Trim(), out contentLength);
                                break;
                            }
                        }

                        int currentBodyLength = totalRead - bodyStartIdx;
                        // Eğer gövdenin tamamı gelmediyse okumaya devam et
                        while (currentBodyLength < contentLength && totalRead < buffer.Length)
                        {
                            int read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead);
                            if (read <= 0) break;
                            totalRead += read;
                            currentBodyLength += read;
                        }
                    }

                    SimpleHttpContext context = ParseRequest(buffer, totalRead);
                    context.Request.RemoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

                    ProcessRequest(context);

                    // Yanıtı gönder
                    await SendResponse(stream, context.Response);
                }
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[AdminServer] Bağlantı işleme hatası: {ex.Message}");
            }
        });
    }

    private SimpleHttpContext ParseRequest(byte[] data, int length)
    {
        var context = new SimpleHttpContext();
        string requestStr = Encoding.UTF8.GetString(data, 0, length);
        string[] lines = requestStr.Split(new[] { "\r\n" }, StringSplitOptions.None);

        if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
        {
            string[] firstLine = lines[0].Split(' ');
            if (firstLine.Length >= 2)
            {
                context.Request.Method = firstLine[0];
                string fullPath = firstLine[1];

                if (fullPath.Contains("?"))
                {
                    int queryIdx = fullPath.IndexOf("?");
                    context.Request.Path = fullPath.Substring(0, queryIdx);
                    string query = fullPath.Substring(queryIdx + 1);
                    foreach (var part in query.Split('&'))
                    {
                        var kv = part.Split('=');
                        if (kv.Length == 2) context.Request.QueryString[kv[0]] = WebUtility.UrlDecode(kv[1]);
                    }
                }
                else
                {
                    context.Request.Path = fullPath;
                }
            }
        }

        // Headerları ayrıştır
        int bodyStartIdx = -1;
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                // Body başlıyor
                bodyStartIdx = requestStr.IndexOf("\r\n\r\n") + 4;
                break;
            }

            int colonIdx = lines[i].IndexOf(':');
            if (colonIdx > 0)
            {
                string key = lines[i].Substring(0, colonIdx).Trim();
                string val = lines[i].Substring(colonIdx + 1).Trim();
                context.Request.Headers[key] = val;

                if (key.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var c in val.Split(';'))
                    {
                        var kv = c.Trim().Split('=');
                        if (kv.Length == 2) context.Request.Cookies[kv[0]] = kv[1];
                    }
                }
            }
        }

        // Body'yi al
        if (bodyStartIdx != -1 && bodyStartIdx < length)
        {
            int bodyLen = length - bodyStartIdx;
            context.Request.Body = new byte[bodyLen];
            Array.Copy(data, bodyStartIdx, context.Request.Body, 0, bodyLen);
        }

        return context;
    }

    private async Task SendResponse(NetworkStream stream, SimpleHttpResponse response)
    {
        var sb = new StringBuilder();
        sb.Append($"HTTP/1.1 {response.StatusCode} {GetStatusText(response.StatusCode)}\r\n");
        sb.Append($"Content-Type: {response.ContentType}\r\n");
        sb.Append($"Content-Length: {response.OutputStream.Length}\r\n");
        sb.Append("Connection: close\r\n");

        foreach (var header in response.Headers)
            sb.Append($"{header.Key}: {header.Value}\r\n");

        foreach (var cookie in response.CookiesToSet)
            sb.Append($"Set-Cookie: {cookie}\r\n");

        sb.Append("\r\n");

        byte[] headerBytes = Encoding.UTF8.GetBytes(sb.ToString());
        await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

        if (response.OutputStream.Length > 0)
        {
            response.OutputStream.Position = 0;
            await response.OutputStream.CopyToAsync(stream);
        }
    }

    private string GetStatusText(int code)
    {
        switch (code)
        {
            case 200: return "OK";
            case 301: return "Moved Permanently";
            case 302: return "Found";
            case 401: return "Unauthorized";
            case 404: return "Not Found";
            case 500: return "Internal Server Error";
            default: return "OK";
        }
    }

    private void ProcessRequest(SimpleHttpContext? context)
    {
        if (context == null || context.Request == null || context.Response == null) return;
        string path = context.Request.Path;
        SimpleHttpResponse response = context.Response;

        try
        {
            // Güvenlik Kontrolü: Statik dosyalar ve Login API dışındaki her şey yetki gerektirir
            if (!IsAuthorized(context))
            {
                // API istekleri için 401 döndür, sayfalar için login.html'e yönlendir
                if (path.StartsWith("/api/"))
                {
                    if (path != "/api/auth/login")
                    {
                        SendError(response, 401, "Unauthorized");
                        return;
                    }
                }
                else if (path.StartsWith("/invite/"))
                {
                    HandleInviteLink(context);
                    return;
                }
                else if (path != "/login.html" && !IsPublicAsset(path))
                {
                    response.Redirect("/login.html");
                    return;
                }
            }

            if (path.StartsWith("/api/"))
            {
                HandleApiRequest(context);
            }
            else
            {
                ServeStaticFile(context);
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { error = ex.Message }));
            response.ContentType = "application/json";
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
    }

    private void HandleApiRequest(SimpleHttpContext context)
    {
        string path = context.Request.Path.ToLower();
        SimpleHttpResponse response = context.Response;
        object? result = null;

        switch (path)
        {
            case "/api/auth/login":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("username") && data.ContainsKey("password"))
                        {
                            var ip = context.Request.RemoteEndPoint;
                            var account = AdminAccountManager.Authenticate(data["username"], data["password"], ip);
                            if (account != null)
                            {
                                string token = Guid.NewGuid().ToString();
                                _sessions[token] = (account.Username, DateTime.Now.AddHours(24));

                                // Cookie olarak set et
                                response.AppendCookie("admin_session", token, "/", DateTime.Now.AddHours(24), true);
                                result = new { success = true, token = token, username = account.Username, role = account.Role };
                            }
                            else result = new { success = false, message = "Geçersiz kullanıcı adı veya şifre." };
                        }
                    }
                }
                break;

            case "/api/auth/logout":
                {
                    if (context.Request.Cookies.TryGetValue("admin_session", out string? token))
                        _sessions.TryRemove(token, out _);
                    response.AppendCookie("admin_session", "", "/", DateTime.Now.AddDays(-1));
                    result = new { success = true };
                }
                break;

            case "/api/auth/check":
                result = new { success = true, authorized = IsAuthorized(context) };
                break;

            case "/api/status":
                {
                    var process = System.Diagnostics.Process.GetCurrentProcess();
                    result = new
                    {
                        version = Config.Instance.ServerVersion,
                        onlinePlayers = SessionManager.GetSessions().Count,
                        lobbyCount = LobbyManager.Lobbies.Count,
                        uptime = (DateTime.Now - process.StartTime).ToString(@"dd\.hh\:mm\:ss"),
                        memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024 + " MB",
                        cpuUsage = GetCpuUsage(process),
                        threadCount = process.Threads.Count,
                        maintenanceMode = Maintance.MaintanceMode
                    };
                }
                break;

            case "/api/players":
                {
                    var onlineSessions = SessionManager.GetSessions();
                    result = AccountCache.GetAllAccounts().Select(acc =>
                    {
                        var isOnline = onlineSessions.TryGetValue(acc.ID, out var session);
                        var isBanned = BanManager.IsBanned(acc.ID);
                        return new
                        {
                            id = acc.ID,
                            username = acc.Username,
                            ip = isOnline ? (session != null ? session.IP : "Online") : "Offline",
                            lastSeen = isOnline ? "Online" : "Offline",
                            isOnline = isOnline,
                            isBanned = isBanned,
                            level = acc.Level,
                            trophies = acc.Trophy,
                            gems = acc.Gems,
                            coins = acc.Coins
                        };
                    }).ToList();
                }
                break;

            case "/api/announce":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("message"))
                        {
                            string msg = data["message"];
                            string title = data.ContainsKey("title") ? data["title"] : "SUNUCU DUYURUSU";
                            int type = data.ContainsKey("type") ? int.Parse(data["type"]) : 2; // Default banner

                            var packet = new NotificationPacket
                            {
                                Type = (NotficationTypes.NotficationType)type,
                                Title = title,
                                Message = msg,
                                ButtonText = "TAMAM",
                                Url = "",
                                UnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                Sender = "SİSTEM"
                            };

                            var sessions = SessionManager.GetAllSessions();
                            foreach (var session in sessions)
                            {
                                session.Send(packet);
                            }

                            var admin = GetAdminUsername(context);
                            AdminAuditLogger.Log(admin, "Duyuru", "Tüm Oyuncular", msg);

                            result = new { success = true, message = $"{sessions.Count} oyuncuya duyuru ({packet.Type}) gönderildi." };
                        }
                    }
                }
                break;

            case "/api/player/kick":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("id"))
                        {
                            if (int.TryParse(data["id"], out int playerId))
                            {
                                var session = SessionManager.GetSession(playerId);
                                if (session != null)
                                {
                                    session.Close();

                                    var admin = GetAdminUsername(context);
                                    var account = AccountCache.Load(playerId);
                                    AdminAuditLogger.Log(admin, "Kovma (Kick)", account?.Username ?? playerId.ToString(), "Sunucudan atıldı.");

                                    result = new { success = true, message = "Oyuncu başarıyla kovuldu." };
                                }
                                else
                                {
                                    result = new { success = false, message = "Oyuncu şu an online değil." };
                                }
                            }
                            else result = new { success = false, message = "Geçersiz Oyuncu ID." };
                        }
                    }
                }
                break;

            case "/api/player/ban":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("id") && data.ContainsKey("reason"))
                        {
                            if (int.TryParse(data["id"], out int playerId))
                            {
                                string reason = data["reason"];

                                var admin = GetAdminUsername(context);
                                BanManager.BanPlayer(playerId, admin, reason, true);

                                var account = AccountCache.Load(playerId);
                                AdminAuditLogger.Log(admin, "Yasaklama (Ban)", account?.Username ?? playerId.ToString(), $"Sebep: {reason}");

                                result = new { success = true, message = "Oyuncu yasaklandı." };
                            }
                            else result = new { success = false, message = "Geçersiz Oyuncu ID." };
                        }
                    }
                }
                break;

            case "/api/player/unban":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("id"))
                        {
                            if (int.TryParse(data["id"], out int playerId))
                            {
                                var admin = GetAdminUsername(context);

                                BanManager.UnbanPlayer(playerId, admin, "Panel üzerinden kaldırıldı.");

                                var account = AccountCache.Load(playerId);
                                AdminAuditLogger.Log(admin, "Yasak Kaldırma", account?.Username ?? playerId.ToString(), "Yasak elle kaldırıldı.");

                                result = new { success = true, message = "Yasak kaldırıldı." };
                            }
                            else result = new { success = false, message = "Geçersiz Oyuncu ID." };
                        }
                    }
                }
                break;

            case "/api/admin/accounts":
                result = AdminAccountManager.GetAccounts().Select(a => new
                {
                    username = a.Username,
                    role = a.Role,
                    createdAt = a.CreatedAt.ToString("g"),
                    lastLogin = a.LastLogin == DateTime.MinValue ? "Hiç yok" : a.LastLogin.ToString("g"),
                    lastIp = a.LastIp
                }).ToList();
                break;

            case "/api/admin/create":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("username") && data.ContainsKey("password"))
                        {
                            string user = data["username"];
                            string pass = data["password"];
                            string role = (data.ContainsKey("role") && data["role"] != null) ? data["role"] : "Admin";
                            bool success = AdminAccountManager.CreateAccount(user, pass, role);

                            if (success)
                            {
                                var admin = GetAdminUsername(context);
                                AdminAuditLogger.Log(admin, "Yeni Admin", user, $"Rol: {role}");
                            }

                            result = new { success = success, message = success ? "Admin hesabı oluşturuldu." : "Bu kullanıcı adı zaten alınmış." };
                        }
                    }
                }
                break;

            case "/api/admin/delete":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("username") && data["username"] != null)
                        {
                            string user = data["username"];
                            bool success = AdminAccountManager.DeleteAccount(user);

                            if (success)
                            {
                                var admin = GetAdminUsername(context);
                                AdminAuditLogger.Log(admin, "Admin Silindi", user, "Admin hesabı silindi.");
                            }

                            result = new { success = success, message = success ? "Admin silindi." : "Bu hesap silinemez veya bulunamadı." };
                        }
                    }
                }
                break;

            case "/api/admin/logs":
                result = AdminAuditLogger.GetLogs().Select(l => new
                {
                    time = l.Timestamp.ToString("HH:mm:ss"),
                    admin = l.AdminUsername,
                    action = l.Action,
                    target = l.Target,
                    details = l.Details
                }).ToList();
                break;

            case "/api/reports":
                result = ReportManager.GetReports().OrderByDescending(r => r.Timestamp).Select(r => new {
                    id = r.Id,
                    reporterName = r.ReporterName,
                    reporterId = r.ReporterId ?? 0,
                    targetId = r.TargetId ?? 0,
                    targetName = r.TargetName,
                    reason = r.Reason,
                    type = r.Type,
                    clubName = r.ClubName,
                    status = r.Status,
                    time = r.Timestamp.ToString("dd.MM.yyyy HH:mm"),
                    context = r.Context.Select(m => {
                        int sid = m.SenderId;
                        if (sid == 0) {
                            var acc = AccountManager.FindAccountByUsername(m.SenderName ?? "");
                            if (acc != null) sid = acc.ID;
                        }
                        return new {
                            senderId = sid,
                            senderName = m.SenderName,
                            content = m.Content,
                            time = m.Time
                        };
                    })
                }).ToList();
                break;

            case "/api/reports/resolve":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("id"))
                        {
                            bool success = ReportManager.ResolveReport(data["id"]);
                            var admin = GetAdminUsername(context);
                            AdminAuditLogger.Log(admin, "Rapor Çözüldü", data["id"], "Rapor admin tarafından incelendi.");
                            result = new { success = success };
                        }
                    }
                }
                break;

            case "/api/reports/delete":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("id"))
                        {
                            bool success = ReportManager.DeleteReport(data["id"]);
                            var admin = GetAdminUsername(context);
                            AdminAuditLogger.Log(admin, "Rapor Silindi", data["id"], "Rapor listeden kaldırıldı.");
                            result = new { success = success };
                        }
                    }
                }
                break;

            case "/api/market/analytics":
                {
                    var allAccounts = AccountCache.GetCachedAccounts().Values.ToList();
                    var totalGems = allAccounts.Sum(a => (long)a.Gems);
                    var totalCoins = allAccounts.Sum(a => (long)a.Coins);
                    var topRich = allAccounts.OrderByDescending(a => a.Gems).Take(10).Select(a => new
                    {
                        username = a.Username,
                        gems = a.Gems,
                        coins = a.Coins
                    }).ToList();

                    result = new
                    {
                        totalGems = totalGems,
                        totalCoins = totalCoins,
                        playerCount = allAccounts.Count,
                        topRich = topRich
                    };
                }
                break;

            case "/api/market/all":
                result = new
                {
                    items = ShopManager.GetMarketItems(),
                    offers = ShopManager.GetOffers()
                };
                break;

            case "/api/market/item/add":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var item = JsonConvert.DeserializeObject<MarketItemData>(body);
                        if (item != null)
                        {
                            ShopManager.AddItem(item);
                            var admin = GetAdminUsername(context);
                            AdminAuditLogger.Log(admin, "Market Ürün Ekleme", item.ItemName, $"{item.Count} adet, {item.BasePrice} Fiyat");
                            result = new { success = true };
                        }
                    }
                }
                break;

            case "/api/market/item/remove":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("id"))
                        {
                            int id = int.Parse(data["id"]);
                            ShopManager.RemoveItem(id);
                            var admin = GetAdminUsername(context);
                            AdminAuditLogger.Log(admin, "Market Ürün Silme", id.ToString(), "Ürün marketten kaldırıldı.");
                            result = new { success = true };
                        }
                    }
                }
                break;

            case "/api/market/offer/add":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var offer = JsonConvert.DeserializeObject<MarketOfferData>(body);
                        if (offer != null)
                        {
                            ShopManager.AddOffer(offer);
                            var admin = GetAdminUsername(context);
                            AdminAuditLogger.Log(admin, "Market Teklif Ekleme", offer.Title, $"{offer.Count} adet, {offer.BasePrice} Fiyat");
                            result = new { success = true };
                        }
                    }
                }
                break;

            case "/api/market/offer/remove":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("id"))
                        {
                            int id = int.Parse(data["id"]);
                            ShopManager.RemoveOffer(id);
                            var admin = GetAdminUsername(context);
                            AdminAuditLogger.Log(admin, "Market Teklif Silme", id.ToString(), "Teklif marketten kaldırıldı.");
                            result = new { success = true };
                        }
                    }
                }
                break;

            case "/api/player/update":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("id"))
                        {
                            if (int.TryParse(data["id"], out int playerId))
                            {
                                var account = AccountCache.Load(playerId);
                                if (account != null)
                                {
                                    int? level = data.TryGetValue("level", out var l) ? int.Parse(l) : null;
                                    int? trophies = data.TryGetValue("trophies", out var t) ? int.Parse(t) : null;
                                    int? gems = data.TryGetValue("gems", out var g) ? int.Parse(g) : null;
                                    int? coins = data.TryGetValue("coins", out var c) ? int.Parse(c) : null;

                                    var session = SessionManager.GetSession(playerId);
                                    var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
                                    logic.UpdateStats(level, gems, coins, trophies);

                                    result = new { success = true, message = "Oyuncu verileri güncellendi." };
                                }
                                else result = new { success = false, message = "Oyuncu bulunamadı." };
                            }
                            else result = new { success = false, message = "Geçersiz Oyuncu ID." };
                        }
                    }
                }
                break;

            case "/api/player/mute":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("id") && data.ContainsKey("minutes"))
                        {
                            if (int.TryParse(data["id"], out int playerId))
                            {
                                int minutes = int.Parse(data["minutes"]);
                                var account = AccountCache.Load(playerId);
                                if (account != null)
                                {
                                    var session = SessionManager.GetSession(playerId);
                                    var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
                                    logic.Mute(TimeSpan.FromMinutes(minutes));
                                    result = new { success = true, message = "Oyuncu susturuldu." };
                                }
                                else result = new { success = false, message = "Oyuncu bulunamadı." };
                            }
                            else result = new { success = false, message = "Geçersiz Oyuncu ID." };
                        }
                    }
                }
                break;

            case "/api/player/unmute":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("id"))
                        {
                            if (int.TryParse(data["id"], out int playerId))
                            {
                                var account = AccountCache.Load(playerId);
                                if (account != null)
                                {
                                    var session = SessionManager.GetSession(playerId);
                                    var logic = session?.Logic ?? new Logic.AccountLogic(account, session);
                                    logic.Unmute();
                                    result = new { success = true, message = "Oyuncunun susturması kaldırıldı." };
                                }
                                else result = new { success = false, message = "Oyuncu bulunamadı." };
                            }
                            else result = new { success = false, message = "Geçersiz Oyuncu ID." };
                        }
                    }
                }
                break;

            case "/api/traffic/details":
                {
                    var report = TrafficMonitor.GetDetailedReport();
                    if (report == null) result = new { error = "Veri alınamadı" };
                    else result = report;
                }
                break;

            case "/api/player/profile":
                if (context.Request.QueryString.ContainsKey("id"))
                {
                    if (int.TryParse(context.Request.QueryString["id"], out int playerId))
                    {
                        var account = AccountCache.Load(playerId);
                        if (account != null)
                        {
                            var onlineSessions = SessionManager.GetSessions();
                            var isOnline = onlineSessions.TryGetValue(playerId, out var session);

                            result = new
                            {
                                success = true,
                                profile = new
                                {
                                    id = account.ID,
                                    username = account.Username,
                                    level = account.Level,
                                    trophies = account.Trophy,
                                    gems = account.Gems,
                                    coins = account.Coins,
                                    premium = account.Premium,
                                    premiumEndTime = account.PremiumEndTime > DateTime.Now ? account.PremiumEndTime.ToString("dd.MM.yyyy HH:mm") : "Aktif Değil",
                                    avatarId = account.Avatarid,
                                    nameColorId = account.Namecolorid,
                                    clubName = account.ClubName ?? "Kulüp Yok",
                                    clubRole = account.clubRole.ToString(),
                                    roles = account.Roles.Select(r => r.ToString()).ToList(),
                                    friendCount = account.Friends.Count,
                                    ticketCount = account.Tickets.Count,
                                    questCount = account.Quests.Count,
                                    chatBan = account.ChatBan,
                                    ticketBan = account.TicketBan,
                                    muted = account.Muted,
                                    mutedEndTime = account.MutedEndTime > DateTime.Now ? account.MutedEndTime.ToString("dd.MM.yyyy HH:mm") : "Aktif Değil",
                                    email = string.IsNullOrEmpty(account.Email) ? "Bağlı Değil" : (account.Email.Contains("@") ? account.Email.Substring(0, 2) + "***" + account.Email.Substring(account.Email.IndexOf("@")) : "Hatalı Format"),
                                    lastLogin = account.LastLogin.ToString("dd.MM.yyyy HH:mm"),
                                    ip = isOnline ? (session != null ? session.IP : "Online") : (account.LastIp ?? "Bilinmiyor"),
                                    device = account.Device ?? "Bilinmiyor",
                                    status = isOnline ? "Online" : "Offline",
                                    banStatus = account.Banned ? "Yasaklı" : "Aktif",
                                    banReason = account.Banreason ?? "",
                                    banEndTime = account.Banned ? (BanManager.GetBanInfo(playerId)?.Perma == true ? "Kalıcı" : BanManager.GetBanInfo(playerId)?.BanFinishDate?.ToString("dd.MM.yyyy HH:mm") ?? "Bilinmiyor") : "Yok",
                                    // Yeni Alanlar
                                    dil = account.Dil ?? "tr",
                                    hasPushToken = !string.IsNullOrEmpty(account.FBNToken),
                                    friendRequestCount = account.Requests.Count,
                                    banHistoryCount = account.BanHistory.Count,
                                    banHistory = account.BanHistory.Select(b => new {
                                        time = b.BanDate.ToString("dd.MM.yyyy HH:mm"),
                                        banner = b.BannedBy ?? "Sistem",
                                        reason = b.Reason ?? "Belirtilmedi",
                                        finishDate = b.BanFinishDate?.ToString("dd.MM.yyyy HH:mm") ?? "Süresiz",
                                        isPerma = b.Perma
                                    }).ToList(),
                                    inboxCount = account.inboxesNotfications.Count,
                                    notificationSettings = new
                                    {
                                        bestFriends = account.SendOnlineBestFriendNotification,
                                        events = account.SendNewEventNotification,
                                        invites = account.SendInviteNotification,
                                        rewards = account.SendClaimRewardNotification
                                    }
                                }
                            };
                        }
                        else result = new { success = false, message = "Oyuncu bulunamadı." };
                    }
                    else result = new { success = false, message = "Geçersiz Oyuncu ID." };
                }
                break;

            case "/api/traffic/history":
                result = new { history = TrafficMonitor.GetHistory() };
                break;

            case "/api/logs":
                result = new { logs = GetLastLogs("genellog.txt", 100) };
                break;

            case "/api/client/errors":
                result = ClientErrorManager.GetErrors();
                break;

            case "/api/client/errors/clear":
                if (context.Request.Method == "POST")
                {
                    ClientErrorManager.ClearLogs();
                    var admin = GetAdminUsername(context);
                    AdminAuditLogger.Log(admin, "Hata Günlüğü Temizlendi", "ClientErrors", "Tüm istemci hata kayıtları silindi.");
                    result = new { success = true, message = "Tüm hatalar temizlendi." };
                }
                break;

            case "/api/broadcast/alert":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("title") && data.ContainsKey("message"))
                        {
                            string title = data["title"];
                            string msg = data["message"];
                            int targetId = data.ContainsKey("id") ? int.Parse(data["id"]) : 0;

                            var packet = new NotificationPacket
                            {
                                Type = NotficationTypes.NotficationType.banner, // Byte 1
                                Title = title,
                                Message = msg,
                                ButtonText = "TAMAM",
                                Url = " "
                            };

                            var admin = GetAdminUsername(context);
                            
                            if (targetId == 0)
                            {
                                var sessions = SessionManager.GetSessions().Values;
                                foreach (var s in sessions) s.Send(packet);
                                AdminAuditLogger.Log(admin, "Genel Hata Yayınla", "Herkes", $"{title}: {msg}");
                                result = new { success = true, message = "Duyuru tüm oyunculara gönderildi." };
                            }
                            else
                            {
                                var session = SessionManager.GetSession(targetId);
                                if (session != null)
                                {
                                    session.Send(packet);
                                    AdminAuditLogger.Log(admin, "Özel Hata Yayınla", targetId.ToString(), $"{title}: {msg}");
                                    result = new { success = true, message = $"Hata oyuncuya (#{targetId}) gönderildi." };
                                }
                                else result = new { success = false, message = "Oyuncu şu an çevrimiçi değil." };
                            }
                        }
                    }
                }
                break;

            case "/api/maintenance/toggle":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        bool enable = data != null && data.ContainsKey("enabled") && bool.Parse(data["enabled"]);
                        bool panic = data != null && data.ContainsKey("panic") && bool.Parse(data["panic"]);

                        if (enable)
                        {
                            // StartMaintance is a blocking call in some implementations, 
                            // run in thread if it has Sleep
                            new Thread(() => Maintance.StartMaintance(TimeSpan.FromHours(2), panic)).Start();
                            result = new { success = true, message = "Bakım modu başlatılıyor..." };
                        }
                        else
                        {
                            Maintance.finishMaintence();
                            result = new { success = true, message = "Bakım modu sona erdirildi." };
                        }
                    }
                }
                break;

            case "/api/command":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var cmdData = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (cmdData != null && cmdData.ContainsKey("command"))
                        {
                            string cmd = cmdData["command"];
                            result = ExecuteServerCommand(cmd);
                        }
                    }
                }
                break;

            case "/api/support/tickets":
                {
                    var activeTickets = BotManager.istance.TicketSystem.GetActiveTickets();
                    result = activeTickets.Select(t => new
                    {
                        id = t.ID,
                        no = t.NO,
                        username = t.Username,
                        playerId = t.PlayerID,
                        title = t.Title,
                        isClosed = t.IsClosed,
                        createdAt = t.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                        lastMessage = t.ticketMessages.LastOrDefault()?.Message ?? "Mesaj yok",
                        messages = t.ticketMessages.Select(m => new
                        {
                            name = m.Name,
                            message = m.Message,
                            time = m.time.ToString("HH:mm")
                        }).ToList()
                    }).ToList();
                }
                break;

            case "/api/support/reply":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("ticketId") && data.ContainsKey("message"))
                        {
                            int ticketId = int.Parse(data["ticketId"]);
                            string msg = data["message"];
                            var ticket = BotManager.istance.TicketSystem.GetTicketDataByTicketID(ticketId);
                            if (ticket != null)
                            {
                                BotManager.istance.TicketSystem.SendTicketMessage(ticket.PlayerID, $"(Admin) {msg}", ticketId);

                                // Lokal log ekleme (Cevap anında panelde görünmesi için)
                                var adminMsg = new TicketMessage { Name = "Admin", Message = msg, time = DateTime.Now };
                                ticket.ticketMessages.Add(adminMsg);

                                result = new { success = true, message = "Cevap gönderildi." };
                            }
                            else result = new { success = false, message = "Bilet bulunamadı." };
                        }
                    }
                }
                break;

            case "/api/support/close":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                        if (data != null && data.ContainsKey("ticketId"))
                        {
                            int ticketId = int.Parse(data["ticketId"]);
                            string reason = data.ContainsKey("reason") ? data["reason"] : "Admin tarafından kapatıldı.";
                            var ticket = BotManager.istance.TicketSystem.GetTicketDataByTicketID(ticketId);
                            if (ticket != null)
                            {
                                BotManager.istance.TicketSystem.CloseTicketAsync(ticket.channelid, reason, ticketId);
                                result = new { success = true, message = "Bilet kapatıldı." };
                            }
                            else result = new { success = false, message = "Bilet bulunamadı." };
                        }
                    }
                }
                break;

            case "/api/dynamicconfig":
                result = DynamicConfigManager.Config;
                break;

            case "/api/dynamicconfig/update":
                if (context.Request.Method == "POST")
                {
                    using (var reader = new StreamReader(new MemoryStream(context.Request.Body), Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd();
                        var newConfig = JsonConvert.DeserializeObject<DynamicConfig>(body);
                        if (newConfig != null)
                        {
                            DynamicConfigManager.Update(newConfig);
                            result = new { success = true, message = "Yapılandırma güncellendi." };
                        }
                        else result = new { success = false, message = "Geçersiz veri." };
                    }
                }
                break;

            default:
                response.StatusCode = 404;
                result = new { error = "Endpoint not found" };
                break;
        }

        string json = JsonConvert.SerializeObject(result ?? new { success = true });
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    private object ExecuteServerCommand(string cmd)
    {
        Logger.genellog($"[AdminServer] Web üzerinden komut alındı: {cmd}");
        return new { success = true, message = "Komut alındı (Sunucu konsolunda işleniyor olabilir)" };
    }

    private string GetLastLogs(string fileName, int lineCount)
    {
        try
        {
            if (!File.Exists(fileName)) return "Log dosyası bulunamadı.";

            // Dosya başka bir işlem tarafından kilitli olabilir (Logger yazarken)
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                var lines = new List<string>();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                    if (lines.Count > lineCount * 2) // Hafızayı koru
                    {
                        lines.RemoveRange(0, lineCount);
                    }
                }

                int start = Math.Max(0, lines.Count - lineCount);
                return string.Join("\n", lines.Skip(start));
            }
        }
        catch (Exception ex)
        {
            return $"Log okuma hatası: {ex.Message}";
        }
    }

    private void ServeStaticFile(SimpleHttpContext? context)
    {
        if (context == null || context.Request == null || context.Response == null) return;
        string path = context.Request.Path;
        if (path == "/") path = "/index.html";

        string filePath = Path.Combine(_adminPath, path.TrimStart('/'));
        SimpleHttpResponse response = context.Response;

        Console.WriteLine($"[AdminServer] {context.Request.Method} {path} -> {filePath}");

        if (File.Exists(filePath))
        {
            byte[] buffer = File.ReadAllBytes(filePath);
            response.ContentType = GetContentType(Path.GetExtension(filePath));
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        else
        {
            response.StatusCode = 404;
            byte[] errorBuffer = Encoding.UTF8.GetBytes($"404 - File Not Found at {filePath}");
            response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
        }
    }

    private double GetCpuUsage(System.Diagnostics.Process process)
    {
        var now = DateTime.Now;
        if ((now - _lastCpuCheck).TotalSeconds < 1) return _currentCpuUsage;

        var currentProcessorTime = process.TotalProcessorTime;
        if (_lastCpuCheck != DateTime.MinValue)
        {
            var cpuUsedMs = (currentProcessorTime - _lastProcessorTime).TotalMilliseconds;
            var elapsedMs = (now - _lastCpuCheck).TotalMilliseconds;
            _currentCpuUsage = Math.Round((cpuUsedMs / (Environment.ProcessorCount * elapsedMs)) * 100, 1);
        }

        _lastCpuCheck = now;
        _lastProcessorTime = currentProcessorTime;
        return _currentCpuUsage;
    }

    private string GetContentType(string extension)
    {
        switch (extension.ToLower())
        {
            case ".html": return "text/html";
            case ".css": return "text/css";
            case ".js": return "application/javascript";
            case ".json": return "application/json";
            case ".png": return "image/png";
            case ".jpg": return "image/jpeg";
            default: return "application/octet-stream";
        }
    }

    private bool IsAuthorized(SimpleHttpContext context)
    {
        if (context.Request.Cookies.TryGetValue("admin_session", out string? token))
        {
            if (_sessions.TryGetValue(token, out var session))
            {
                if (DateTime.Now < session.Expiry) return true;
                _sessions.TryRemove(token, out _); // Süresi dolmuşsa temizle
            }
        }
        return false;
    }

    private bool IsPublicAsset(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext == ".css" || ext == ".js" || ext == ".png" || ext == ".jpg" || ext == ".ico";
    }

    private void SendError(SimpleHttpResponse response, int code, string message)
    {
        response.StatusCode = code;
        byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { error = message }));
        response.ContentType = "application/json";
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    private string GetAdminUsername(SimpleHttpContext context)
    {
        if (context.Request.Cookies.TryGetValue("admin_session", out string? token))
        {
            if (_sessions.TryGetValue(token, out var session))
            {
                return session.Username;
            }
        }
        return "Bilinmiyor";
    }

    private void HandleInviteLink(SimpleHttpContext context)
    {
        string path = context.Request.Path;
        string token = path.StartsWith("/invite/") ? path.Substring("/invite/".Length) : "";
        SimpleHttpResponse response = context.Response;

        var invite = InviteManager.GetInvite(token);
        string html;

        if (invite != null)
        {
            string clientIp = context.Request.RemoteEndPoint;
            Logger.genellog($"[Invite] Token: {token} erişildi. IP: {clientIp} (Kurucu: {invite.OwnerID}, Tip: {invite.Type}, Tık: {invite.Clicks})");

            string typeStr = invite.Type == InviteType.Team ? "Takım" : "Arkadaşlık";
            string ownerName = AccountCache.Load(invite.OwnerID)?.Username ?? "Bilinmiyor";

            html = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Davet Edildin! - Oyun Daveti</title>
    <link href='https://fonts.googleapis.com/css2?family=Outfit:wght@400;700&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary: #00ffa3;
            --secondary: #bc00ff;
            --bg: #0f172a;
            --card-bg: rgba(30, 41, 59, 0.7);
        }}
        
        * {{ margin: 0; padding: 0; box-sizing: border-box; font-family: 'Outfit', sans-serif; }}
        
        body {{
            background: #0f172a;
            background: radial-gradient(circle at top right, #1e1b4b, #0f172a);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            color: white;
            overflow: hidden;
        }}

        .background-blob {{
            position: absolute;
            width: 500px;
            height: 500px;
            background: linear-gradient(45deg, var(--primary), var(--secondary));
            filter: blur(150px);
            opacity: 0.15;
            z-index: 0;
            animation: move 20s infinite alternate;
        }}

        @keyframes move {{
            from {{ transform: translate(-20%, -20%); }}
            to {{ transform: translate(20%, 20%); }}
        }}

        .card {{
            background: var(--card-bg);
            backdrop-filter: blur(20px);
            -webkit-backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            padding: 3rem;
            border-radius: 2rem;
            text-align: center;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
            max-width: 450px;
            width: 90%;
            z-index: 10;
            animation: fadeIn 0.8s ease-out;
        }}

        @keyframes fadeIn {{
            from {{ opacity: 0; transform: translateY(20px); }}
            to {{ opacity: 1; transform: translateY(0); }}
        }}

        .avatar-container {{
            width: 100px;
            height: 100px;
            margin: 0 auto 1.5rem;
            background: linear-gradient(45deg, var(--primary), var(--secondary));
            padding: 4px;
            border-radius: 50%;
            box-shadow: 0 0 20px rgba(0, 255, 163, 0.3);
        }}

        .avatar {{
            width: 100%;
            height: 100%;
            background: #1e293b;
            border-radius: 50%;
            display: flex;
            justify-content: center;
            align-items: center;
            font-size: 2.5rem;
        }}

        h1 {{ font-size: 2rem; margin-bottom: 0.5rem; background: linear-gradient(to right, #fff, #94a3b8); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }}
        .invite-text {{ color: #94a3b8; line-height: 1.6; margin-bottom: 1.5rem; }}
        .invite-text span {{ color: var(--primary); font-weight: bold; }}

        #status-text {{ font-size: 0.9rem; color: var(--primary); margin-bottom: 1.5rem; min-height: 1.2rem; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; }}

        .btn {{
            display: block;
            background: linear-gradient(45deg, var(--primary), #00d4ff);
            color: #0f172a;
            padding: 1.2rem;
            border-radius: 1rem;
            text-decoration: none;
            font-weight: 700;
            font-size: 1.1rem;
            transition: all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
            box-shadow: 0 10px 15px -3px rgba(0, 255, 163, 0.4);
            text-transform: uppercase;
            letter-spacing: 1px;
        }}

        .btn:hover {{ transform: scale(1.05); box-shadow: 0 20px 25px -5px rgba(0, 255, 163, 0.5); }}
        
        .pulse {{ animation: btnPulse 1.5s infinite; }}
        @keyframes btnPulse {{
            0% {{ transform: scale(1); }}
            50% {{ transform: scale(1.05); box-shadow: 0 0 30px rgba(0, 255, 163, 0.6); }}
            100% {{ transform: scale(1); }}
        }}

        .footer {{ margin-top: 2rem; font-size: 0.85rem; color: #64748b; }}
        .footer a {{ color: var(--primary); text-decoration: none; }}
    </style>
</head>
<body>
    <div class='background-blob'></div>
    <div class='card'>
        <div class='avatar-container'>
            <div class='avatar'>🎮</div>
        </div>
        <h1>Davet Edildin!</h1>
        <p class='invite-text'>
            <span>{ownerName}</span> seni bir <span>{typeStr}</span> davetine çağırdı. Heyecana ortak olmak için hemen katıl!
        </p>
        <div id='status-text'>Bağlantı hazırlanıyor...</div>
        <a href='com.ardagamedevtest://invite/{token}' id='join-btn' class='btn'>OYUNA KATIL</a>
        <p class='footer'>Eğer oyun yüklü değilse <a href='#'>buradan</a> indirebilirsin.</p>
    </div>

    <script>
        const url = 'oyun://invite/{token}';
        const statusText = document.getElementById('status-text');
        const joinBtn = document.getElementById('join-btn');
        
        let countdown = 3;
        
        function startAutoJoin() {{
            const timer = setInterval(() => {{
                countdown--;
                if (countdown <= 0) {{
                    clearInterval(timer);
                    statusText.innerText = 'Oyun başlatılıyor...';
                    window.location.href = url;
                    joinBtn.classList.add('pulse');
                    
                    // Fallback: If still on page after 5s, show manual prompt
                    setTimeout(() => {{
                        statusText.innerText = 'Oyun açılmadı mı? Yukarıdaki butona tıklayın.';
                    }}, 5000);
                }} else {{
                    statusText.innerText = countdown + ' saniye içinde otomatik katılınıyor...';
                }}
            }}, 1000);
        }}
        
        // Auto start
        window.onload = () => {{
            setTimeout(startAutoJoin, 500); // Small delay for fade-in
        }};
    </script>
</body>
</html>";
        }
        else
        {
            html = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Geçersiz Davet - Oyun Daveti</title>
    <link href='https://fonts.googleapis.com/css2?family=Outfit:wght@400;700&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary: #ff4b2b;
            --secondary: #ff416c;
            --bg: #0f172a;
            --card-bg: rgba(30, 41, 59, 0.7);
        }}
        
        * {{ margin: 0; padding: 0; box-sizing: border-box; font-family: 'Outfit', sans-serif; }}
        
        body {{
            background: #0f172a;
            background: radial-gradient(circle at top right, #1e1b4b, #0f172a);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            color: white;
            overflow: hidden;
        }}

        .card {{
            background: var(--card-bg);
            backdrop-filter: blur(20px);
            -webkit-backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            padding: 3rem;
            border-radius: 2rem;
            text-align: center;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
            max-width: 450px;
            width: 90%;
            z-index: 10;
        }}

        .icon {{ font-size: 4rem; margin-bottom: 1rem; color: var(--primary); }}
        h1 {{ font-size: 1.8rem; margin-bottom: 1rem; color: white; }}
        .text {{ color: #94a3b8; line-height: 1.6; margin-bottom: 2rem; }}

        .btn {{
            display: inline-block;
            background: rgba(255, 255, 255, 0.1);
            color: white;
            padding: 1rem 2rem;
            border-radius: 1rem;
            text-decoration: none;
            font-weight: 700;
            border: 1px solid rgba(255, 255, 255, 0.2);
            transition: all 0.3s;
        }}

        .btn:hover {{ background: rgba(255, 255, 255, 0.2); }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='icon'>⚠️</div>
        <h1>Davet Geçersiz</h1>
        <p class='text'>Bu davet linkinin süresi dolmuş veya iptal edilmiş olabilir. Lütfen arkadaşından yeni bir davet iste.</p>
        <a href='/' class='btn'>ANA SAYFAYA DÖN</a>
    </div>
</body>
</html>";
        }

        byte[] buffer = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html";
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }
}
