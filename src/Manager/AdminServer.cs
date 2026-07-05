using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

public class AdminServer
{
    private bool _isRunning;
    private DateTime _lastCpuCheck = DateTime.MinValue;
    private TimeSpan _lastProcessorTime;
    private double _currentCpuUsage = 0;

    public AdminServer()
    {
        AdminAccountManager.Initialize();
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        HttpRouter.RegisterControllers();
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
                    byte[] buffer = new byte[65536];
                    int totalRead = initialData.Length;
                    Array.Copy(initialData, 0, buffer, 0, initialData.Length);

                    while (totalRead < buffer.Length && !Encoding.UTF8.GetString(buffer, 0, totalRead).Contains("\r\n\r\n"))
                    {
                        int read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead);
                        if (read <= 0) break;
                        totalRead += read;
                    }

                    string requestStr = Encoding.UTF8.GetString(buffer, 0, totalRead);
                    int bodyStartIdx = requestStr.IndexOf("\r\n\r\n");
                    if (bodyStartIdx != -1)
                    {
                        bodyStartIdx += 4;
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

                    await SendResponse(stream, context);
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

        int bodyStart = -1;
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                bodyStart = requestStr.IndexOf("\r\n\r\n") + 4;
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

        if (bodyStart != -1 && bodyStart < length)
        {
            int bodyLen = length - bodyStart;
            context.Request.Body = new byte[bodyLen];
            Array.Copy(data, bodyStart, context.Request.Body, 0, bodyLen);
        }

        return context;
    }

    private void ProcessRequest(SimpleHttpContext? context)
    {
        if (context == null) return;

        try
        {
            if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 200;
                return;
            }

            string path = context.Request.Path;
            bool isApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
            bool isInvite = path.StartsWith("/invite/", StringComparison.OrdinalIgnoreCase);

            if (!isApi && !isInvite)
            {
                context.Response.WriteError(404, "Not Found");
                return;
            }

            if (isInvite)
            {
                HandleInviteLink(context);
            }
            else
            {
                HttpRouter.Handle(context);
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            byte[] buf = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { error = ex.Message }));
            context.Response.ContentType = "application/json";
            context.Response.OutputStream.Write(buf, 0, buf.Length);
        }
    }

    private async Task SendResponse(NetworkStream stream, SimpleHttpContext context)
    {
        SimpleHttpResponse response = context.Response;
        var sb = new StringBuilder();
        sb.Append($"HTTP/1.1 {response.StatusCode} {GetStatusText(response.StatusCode)}\r\n");
        sb.Append($"Content-Type: {response.ContentType}\r\n");
        sb.Append($"Content-Length: {response.OutputStream.Length}\r\n");
        sb.Append("Connection: close\r\n");

        string origin = "*";
        if (context.Request.Headers.TryGetValue("Origin", out string? reqOrigin))
            origin = reqOrigin;

        sb.Append($"Access-Control-Allow-Origin: {origin}\r\n");
        sb.Append("Access-Control-Allow-Methods: GET, POST, OPTIONS, PUT, DELETE\r\n");
        sb.Append("Access-Control-Allow-Headers: Content-Type, Authorization, Cookie\r\n");
        sb.Append("Access-Control-Allow-Credentials: true\r\n");

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

    internal double GetCpuUsage(System.Diagnostics.Process process)
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

            html = InvitePageBuilder.BuildValidInvite(ownerName, typeStr, token);
        }
        else
        {
            html = InvitePageBuilder.BuildInvalidInvite();
        }

        byte[] buffer = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html";
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }
}
