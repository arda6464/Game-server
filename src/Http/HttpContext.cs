using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

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

    public void WriteJson(object data)
    {
        string json = JsonConvert.SerializeObject(data ?? new { success = true });
        ContentType = "application/json";
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        OutputStream.Write(buffer, 0, buffer.Length);
    }

    public void WriteError(int statusCode, string message)
    {
        StatusCode = statusCode;
        WriteJson(new { error = message });
    }
}

public class SimpleHttpContext
{
    public SimpleHttpRequest Request { get; set; } = new SimpleHttpRequest();
    public SimpleHttpResponse Response { get; set; } = new SimpleHttpResponse();
    public string AdminUsername { get; set; } = "Bilinmiyor";
}
